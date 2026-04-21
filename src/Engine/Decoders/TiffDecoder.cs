using System.Buffers.Binary;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// Pure C# TIFF decoder — baseline uncompressed RGB / RGBA only, first-IFD only (multi-page TIFFs).
/// Supports little-endian and big-endian byte order.  Real-world TIFFs are usually more varied
/// (LZW, PackBits, tiles, CMYK, 16-bit), but baseline-uncompressed covers Mac screenshots,
/// scanner output, and our synthetic fixture.  For v1 we deliberately scope low — see PRD.
/// </summary>
internal sealed class TiffDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.Length < 8) throw new DecoderException("tiff: header too short");
        bool little;
        if (sourceBytes[0] == 'I' && sourceBytes[1] == 'I') little = true;
        else if (sourceBytes[0] == 'M' && sourceBytes[1] == 'M') little = false;
        else throw new DecoderException("tiff: missing II/MM magic");

        var magic = ReadU16(sourceBytes[2..4], little);
        if (magic != 42) throw new DecoderException($"tiff: expected magic 42, got {magic}");

        var ifd0 = ReadU32(sourceBytes[4..8], little);
        if (ifd0 + 2 > sourceBytes.Length) throw new DecoderException("tiff: IFD0 offset out of range");

        int entries = ReadU16(sourceBytes.Slice((int)ifd0, 2), little);
        int width = 0, height = 0, compression = 1, photometric = 2, samples = 1;
        uint[] stripOffsets = [];
        uint[] stripLengths = [];
        var bitsPerSample = 8;

        for (var i = 0; i < entries; i++)
        {
            var ofs = (int)ifd0 + 2 + i * 12;
            var tag = ReadU16(sourceBytes.Slice(ofs, 2), little);
            var type = ReadU16(sourceBytes.Slice(ofs + 2, 2), little);
            var count = (int)ReadU32(sourceBytes.Slice(ofs + 4, 4), little);
            var valOfs = ofs + 8;

            switch (tag)
            {
                case 256: width = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 257: height = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 258: bitsPerSample = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 259: compression = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 262: photometric = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 273: stripOffsets = ReadTagArray(sourceBytes, valOfs, type, count, little); break;
                case 277: samples = (int)ReadTagValue(sourceBytes, valOfs, type, little); break;
                case 279: stripLengths = ReadTagArray(sourceBytes, valOfs, type, count, little); break;
            }
        }

        if (compression != 1) throw new DecoderException($"tiff: unsupported compression {compression} (uncompressed only in v1)");
        if (bitsPerSample != 8) throw new DecoderException($"tiff: unsupported bitsPerSample {bitsPerSample}");
        if (samples != 3 && samples != 4) throw new DecoderException($"tiff: unsupported samples {samples}");
        if (photometric != 2) throw new DecoderException($"tiff: unsupported photometric {photometric}");
        if (stripOffsets.Length == 0 || stripLengths.Length != stripOffsets.Length)
            throw new DecoderException("tiff: missing strips");

        var pixels = new byte[width * height * 4];
        var dstIdx = 0;
        var rowsPerStrip = (int)Math.Ceiling(height / (double)stripOffsets.Length);
        for (var s = 0; s < stripOffsets.Length; s++)
        {
            var stripStart = (int)stripOffsets[s];
            var stripLen = (int)stripLengths[s];
            if (stripStart + stripLen > sourceBytes.Length) throw new DecoderException("tiff: strip out of range");
            var stripRows = Math.Min(rowsPerStrip, height - s * rowsPerStrip);
            var stripBytesExpected = stripRows * width * samples;
            if (stripLen < stripBytesExpected) throw new DecoderException("tiff: strip underrun");
            for (var i = 0; i < stripRows * width; i++)
            {
                pixels[dstIdx + 0] = sourceBytes[stripStart + i * samples + 0];
                pixels[dstIdx + 1] = sourceBytes[stripStart + i * samples + 1];
                pixels[dstIdx + 2] = sourceBytes[stripStart + i * samples + 2];
                pixels[dstIdx + 3] = samples == 4 ? sourceBytes[stripStart + i * samples + 3] : (byte)0xFF;
                dstIdx += 4;
            }
        }

        return new DecodedImage { Width = width, Height = height, Pixels = pixels };
    }

    private static uint ReadTagValue(ReadOnlySpan<byte> src, int valOfs, ushort type, bool little) => type switch
    {
        3 => ReadU16(src.Slice(valOfs, 2), little),
        4 => ReadU32(src.Slice(valOfs, 4), little),
        _ => 0u,
    };

    private static uint[] ReadTagArray(ReadOnlySpan<byte> src, int valOfs, ushort type, int count, bool little)
    {
        var elem = type == 3 ? 2 : 4;
        var size = elem * count;
        var start = size <= 4 ? valOfs : (int)ReadU32(src.Slice(valOfs, 4), little);
        var arr = new uint[count];
        for (var j = 0; j < count; j++)
        {
            arr[j] = elem == 2
                ? ReadU16(src.Slice(start + j * 2, 2), little)
                : ReadU32(src.Slice(start + j * 4, 4), little);
        }
        return arr;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> s, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(s) : BinaryPrimitives.ReadUInt16BigEndian(s);
    private static uint ReadU32(ReadOnlySpan<byte> s, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(s) : BinaryPrimitives.ReadUInt32BigEndian(s);
}
