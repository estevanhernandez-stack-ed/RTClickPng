using System.Buffers.Binary;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// Pure C# GIF decoder — first-frame-only per PRD.  Supports standard LZW-compressed
/// frames with global or local color tables.  Multi-frame animations return only frame 0
/// (no warning, per product decision).
/// </summary>
internal sealed class GifDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.Length < 13) throw new DecoderException("gif: header too short");
        if (!(sourceBytes[0] == 'G' && sourceBytes[1] == 'I' && sourceBytes[2] == 'F'))
            throw new DecoderException("gif: missing 'GIF' magic");

        var pos = 6;  // skip signature+version
        var screenWidth = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
        var screenHeight = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
        var packed = sourceBytes[pos++]; pos += 2;  // bgColor + aspect

        byte[]? globalPalette = null;
        if ((packed & 0x80) != 0)
        {
            var gctSize = 1 << ((packed & 0x07) + 1);
            globalPalette = sourceBytes.Slice(pos, gctSize * 3).ToArray();
            pos += gctSize * 3;
        }

        byte? transparentIndex = null;

        while (pos < sourceBytes.Length)
        {
            var b = sourceBytes[pos++];
            if (b == 0x3B) break;  // trailer
            if (b == 0x21)  // extension
            {
                var label = sourceBytes[pos++];
                if (label == 0xF9)  // graphics control
                {
                    var blockSize = sourceBytes[pos++];
                    if (blockSize == 4)
                    {
                        var gcePacked = sourceBytes[pos++];
                        pos += 2;  // delay
                        var tIdx = sourceBytes[pos++];
                        if ((gcePacked & 0x01) != 0) transparentIndex = tIdx;
                        pos++; // block terminator
                    }
                    else pos += blockSize + 1;
                }
                else
                {
                    // skip sub-blocks
                    while (pos < sourceBytes.Length)
                    {
                        var subLen = sourceBytes[pos++];
                        if (subLen == 0) break;
                        pos += subLen;
                    }
                }
                continue;
            }
            if (b == 0x2C)  // image descriptor → decode this frame and stop
            {
                var left = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
                var top = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
                var width = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
                var height = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[pos..(pos + 2)]); pos += 2;
                var imgPacked = sourceBytes[pos++];

                byte[] palette;
                if ((imgPacked & 0x80) != 0)
                {
                    var lctSize = 1 << ((imgPacked & 0x07) + 1);
                    palette = sourceBytes.Slice(pos, lctSize * 3).ToArray();
                    pos += lctSize * 3;
                }
                else
                {
                    palette = globalPalette ?? throw new DecoderException("gif: frame references missing palette");
                }

                var minCodeSize = sourceBytes[pos++];
                // Collect all sub-block bytes
                var buf = new List<byte>();
                while (pos < sourceBytes.Length)
                {
                    var subLen = sourceBytes[pos++];
                    if (subLen == 0) break;
                    for (var k = 0; k < subLen; k++) buf.Add(sourceBytes[pos++]);
                }
                var indexedPixels = LzwDecompress(buf, minCodeSize, width * height);

                // Expand palette to RGBA, honoring transparent index.
                var pixels = new byte[screenWidth * screenHeight * 4];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var idx = indexedPixels[y * width + x];
                        var dstX = left + x;
                        var dstY = top + y;
                        if (dstX < 0 || dstY < 0 || dstX >= screenWidth || dstY >= screenHeight) continue;
                        var di = (dstY * screenWidth + dstX) * 4;
                        pixels[di + 0] = palette[idx * 3 + 0];
                        pixels[di + 1] = palette[idx * 3 + 1];
                        pixels[di + 2] = palette[idx * 3 + 2];
                        pixels[di + 3] = (transparentIndex.HasValue && idx == transparentIndex.Value) ? (byte)0 : (byte)0xFF;
                    }
                }

                return new DecodedImage { Width = screenWidth, Height = screenHeight, Pixels = pixels };
            }

            // Unknown block — bail
            throw new DecoderException($"gif: unknown block 0x{b:X2} at offset {pos - 1}");
        }

        throw new DecoderException("gif: no image descriptor found");
    }

    private static byte[] LzwDecompress(List<byte> compressed, int minCodeSize, int outputSize)
    {
        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = endCode + 1;
        var dict = new List<byte[]>();
        for (var i = 0; i < clearCode; i++) dict.Add(new[] { (byte)i });
        dict.Add(Array.Empty<byte>());   // clear
        dict.Add(Array.Empty<byte>());   // end

        var bits = 0;
        var acc = 0;
        var srcIdx = 0;
        var output = new byte[outputSize];
        var outIdx = 0;
        byte[]? prev = null;

        while (srcIdx < compressed.Count || bits >= codeSize)
        {
            while (bits < codeSize && srcIdx < compressed.Count)
            {
                acc |= compressed[srcIdx++] << bits;
                bits += 8;
            }
            if (bits < codeSize) break;
            var code = acc & ((1 << codeSize) - 1);
            acc >>= codeSize;
            bits -= codeSize;

            if (code == clearCode)
            {
                codeSize = minCodeSize + 1;
                nextCode = endCode + 1;
                dict.RemoveRange(endCode + 1, dict.Count - endCode - 1);
                prev = null;
                continue;
            }
            if (code == endCode) break;

            byte[] entry;
            if (code < dict.Count)
                entry = dict[code];
            else if (code == nextCode && prev != null)
            {
                entry = new byte[prev.Length + 1];
                Buffer.BlockCopy(prev, 0, entry, 0, prev.Length);
                entry[prev.Length] = prev[0];
            }
            else throw new DecoderException("gif: LZW decoder out of sync");

            foreach (var by in entry)
            {
                if (outIdx < output.Length) output[outIdx++] = by;
            }

            if (prev != null)
            {
                var np = new byte[prev.Length + 1];
                Buffer.BlockCopy(prev, 0, np, 0, prev.Length);
                np[prev.Length] = entry[0];
                dict.Add(np);
                nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12) codeSize++;
            }
            prev = entry;
        }
        return output;
    }
}
