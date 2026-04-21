using System.Buffers.Binary;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// Pure C# BMP decoder.  Handles baseline BI_RGB (no compression) for 24/32-bit sources,
/// which is what Windows Paint / Snipping Tool / Print Screen produce.
/// Not a general-purpose BMP decoder — 1/4/8-bit palette and RLE variants are out of scope for v1.
/// </summary>
internal sealed class BmpDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.Length < 54) throw new DecoderException("bmp: header too short");
        if (sourceBytes[0] != 'B' || sourceBytes[1] != 'M') throw new DecoderException("bmp: missing 'BM' magic");

        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(sourceBytes[10..14]);
        var dibSize = BinaryPrimitives.ReadInt32LittleEndian(sourceBytes[14..18]);
        if (dibSize < 40) throw new DecoderException($"bmp: unsupported DIB header size {dibSize} (need BITMAPINFOHEADER or later)");

        var width = BinaryPrimitives.ReadInt32LittleEndian(sourceBytes[18..22]);
        var heightRaw = BinaryPrimitives.ReadInt32LittleEndian(sourceBytes[22..26]);
        var bpp = BinaryPrimitives.ReadInt16LittleEndian(sourceBytes[28..30]);
        var compression = BinaryPrimitives.ReadInt32LittleEndian(sourceBytes[30..34]);
        if (compression != 0 && compression != 3)  // BI_RGB or BI_BITFIELDS — accept bitfields for 32-bit
            throw new DecoderException($"bmp: unsupported compression {compression}");
        if (bpp != 24 && bpp != 32) throw new DecoderException($"bmp: unsupported bpp {bpp}");

        var height = Math.Abs(heightRaw);
        var topDown = heightRaw < 0;

        // Row stride is padded to 4 bytes.
        var stride = ((width * bpp + 31) / 32) * 4;
        if (pixelOffset + stride * height > sourceBytes.Length) throw new DecoderException("bmp: pixel data truncated");

        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var srcRow = topDown ? y : height - 1 - y;
            var rowOffset = pixelOffset + srcRow * stride;
            var dstRow = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var pix = rowOffset + x * (bpp / 8);
                // BMP is BGRA (or BGR + implicit alpha)
                pixels[dstRow + x * 4 + 0] = sourceBytes[pix + 2];
                pixels[dstRow + x * 4 + 1] = sourceBytes[pix + 1];
                pixels[dstRow + x * 4 + 2] = sourceBytes[pix + 0];
                pixels[dstRow + x * 4 + 3] = bpp == 32 ? sourceBytes[pix + 3] : (byte)0xFF;
            }
        }

        return new DecodedImage { Width = width, Height = height, Pixels = pixels };
    }
}
