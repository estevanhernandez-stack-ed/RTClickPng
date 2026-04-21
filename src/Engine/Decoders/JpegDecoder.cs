using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RTClickPng.Engine.Color;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// JPEG source decoder.  Uses libjpeg-turbo to decode pixels and hand-rolls APP-segment parsing
/// to extract ICC (APP2 "ICC_PROFILE") and EXIF (APP1 "Exif") before handing off to tjDecompress.
/// </summary>
internal sealed class JpegDecoder : IImageDecoder
{
    public unsafe DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.Length < 4) throw new DecoderException("jpeg: input too short");
        if (sourceBytes[0] != 0xFF || sourceBytes[1] != 0xD8) throw new DecoderException("jpeg: missing SOI marker");

        var (icc, exif) = ScanAppSegments(sourceBytes);

        var handle = LibJpegTurbo.tjInitDecompress();
        if (handle == IntPtr.Zero) throw new DecoderException("jpeg: tjInitDecompress returned null");

        try
        {
            fixed (byte* src = sourceBytes)
            {
                var rc = LibJpegTurbo.tjDecompressHeader3(handle, (IntPtr)src, (nuint)sourceBytes.Length,
                                                         out var width, out var height, out _, out _);
                if (rc != 0) throw new DecoderException($"jpeg: tjDecompressHeader3 returned {rc}");

                var pixels = new byte[width * height * 4];
                fixed (byte* dst = pixels)
                {
                    rc = LibJpegTurbo.tjDecompress2(handle, (IntPtr)src, (nuint)sourceBytes.Length,
                                                   (IntPtr)dst, width, width * 4, height,
                                                   LibJpegTurbo.TJPF_RGBA, 0);
                    if (rc != 0) throw new DecoderException($"jpeg: tjDecompress2 returned {rc}");
                }
                return new DecodedImage
                {
                    Width = width,
                    Height = height,
                    Pixels = pixels,
                    IccProfile = ColorProfile.Sanitize(icc),
                    Exif = exif,
                };
            }
        }
        finally
        {
            LibJpegTurbo.tjDestroy(handle);
        }
    }

    /// <summary>
    /// Walk APPn segments between SOI and SOS, collecting ICC (multi-segment per spec) and EXIF.
    /// </summary>
    private static (byte[]? Icc, byte[]? Exif) ScanAppSegments(ReadOnlySpan<byte> src)
    {
        byte[]? exif = null;
        var iccSegments = new SortedDictionary<int, byte[]>();
        var iccTotal = -1;

        var pos = 2;  // after SOI
        while (pos + 4 <= src.Length)
        {
            if (src[pos] != 0xFF) break;
            var marker = src[pos + 1];
            pos += 2;

            // Stand-alone markers (no length): 0xD0..0xD7 (RST), 0x01, 0x00
            if (marker is (>= 0xD0 and <= 0xD7) or 0x01 or 0x00) continue;
            if (marker == 0xD9) break;   // EOI
            if (marker == 0xDA) break;   // SOS — pixel data starts

            if (pos + 2 > src.Length) break;
            var segLen = BinaryPrimitives.ReadUInt16BigEndian(src.Slice(pos, 2));
            if (segLen < 2 || pos + segLen > src.Length) break;
            var payloadStart = pos + 2;
            var payloadLen = segLen - 2;
            var payload = src.Slice(payloadStart, payloadLen);

            if (marker == 0xE1 && payload.Length >= 6 &&
                payload[0] == 'E' && payload[1] == 'x' && payload[2] == 'i' && payload[3] == 'f' &&
                payload[4] == 0 && payload[5] == 0)
            {
                exif = payload[6..].ToArray();  // raw TIFF stream
            }
            else if (marker == 0xE2 && payload.Length >= 14 &&
                     payload[..12].SequenceEqual(ColorProfile.JpegIccMarker))
            {
                var seqNo = payload[12];
                var total = payload[13];
                if (iccTotal == -1) iccTotal = total;
                iccSegments[seqNo] = payload[14..].ToArray();
            }

            pos += segLen;
        }

        byte[]? icc = null;
        if (iccSegments.Count > 0 && iccTotal > 0 && iccSegments.Count == iccTotal)
        {
            var total = 0;
            foreach (var seg in iccSegments.Values) total += seg.Length;
            icc = new byte[total];
            var off = 0;
            foreach (var seg in iccSegments.Values)
            {
                Buffer.BlockCopy(seg, 0, icc, off, seg.Length);
                off += seg.Length;
            }
        }
        return (icc, exif);
    }
}
