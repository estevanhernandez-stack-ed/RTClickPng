using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RTClickPng.Engine.Color;
using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Encoders;

/// <summary>
/// JPEG encoder via libjpeg-turbo (TurboJPEG API).  Writes quality-92 4:2:0 JPEG.
/// ICC profile embedding: after tjCompress2 returns a bare JPEG, we splice APP2 "ICC_PROFILE"
/// segments right after the SOI marker.  Alpha is composited over white since JPEG has no alpha.
/// </summary>
internal sealed class JpegEncoder : IImageEncoder
{
    /// <summary>
    /// ICC in JPEG is split across multiple APP2 segments, each with a 14-byte header plus up to
    /// 65519 bytes of ICC payload (max APP segment = 65533 − 2 length bytes − 14-byte header).
    /// </summary>
    private const int JpegAppMaxPayload = 65533 - 2;  // 65531
    private const int IccHeaderLen = 14;               // "ICC_PROFILE\0" (12) + seqNo (1) + total (1)
    private const int MaxIccPerSegment = JpegAppMaxPayload - IccHeaderLen;

    public void Encode(DecodedImage image, Stream output)
    {
        // Composite alpha over white — JPEG has no alpha channel.
        var rgba = image.Pixels;
        var flattened = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            var a = rgba[i + 3];
            if (a == 255)
            {
                flattened[i + 0] = rgba[i + 0];
                flattened[i + 1] = rgba[i + 1];
                flattened[i + 2] = rgba[i + 2];
            }
            else
            {
                var aF = a / 255.0;
                flattened[i + 0] = (byte)(rgba[i + 0] * aF + 255 * (1 - aF));
                flattened[i + 1] = (byte)(rgba[i + 1] * aF + 255 * (1 - aF));
                flattened[i + 2] = (byte)(rgba[i + 2] * aF + 255 * (1 - aF));
            }
            flattened[i + 3] = 255;
        }

        var handle = LibJpegTurbo.tjInitCompress();
        if (handle == IntPtr.Zero) throw new EncoderException("jpeg: tjInitCompress returned null");

        byte[] jpegBytes;
        try
        {
            IntPtr jpegBuf = IntPtr.Zero;
            nuint jpegSize = 0;
            int rc;

            unsafe
            {
                fixed (byte* src = flattened)
                {
                    rc = LibJpegTurbo.tjCompress2(
                        handle, (IntPtr)src,
                        image.Width, image.Width * 4, image.Height,
                        LibJpegTurbo.TJPF_RGBA,
                        ref jpegBuf, ref jpegSize,
                        LibJpegTurbo.TJSAMP_420,
                        92, 0);
                }
            }
            if (rc != 0 || jpegBuf == IntPtr.Zero)
                throw new EncoderException($"jpeg: tjCompress2 returned {rc}");

            try
            {
                jpegBytes = new byte[(int)jpegSize];
                Marshal.Copy(jpegBuf, jpegBytes, 0, jpegBytes.Length);
            }
            finally
            {
                LibJpegTurbo.tjFree(jpegBuf);
            }
        }
        finally
        {
            LibJpegTurbo.tjDestroy(handle);
        }

        var icc = ColorProfile.Sanitize(image.IccProfile);
        if (icc is null)
        {
            output.Write(jpegBytes, 0, jpegBytes.Length);
            return;
        }

        // Splice APP2 "ICC_PROFILE" segments after the initial SOI (FF D8) and before any existing
        // segments.  tjCompress2 does not embed ICC, so we never risk duplicate profiles here.
        var totalSegments = (icc.Length + MaxIccPerSegment - 1) / MaxIccPerSegment;
        if (totalSegments > 255)
        {
            // ICC spec caps at 255 segments.  Our sanitize bound (1 MB / 65505 per seg ≈ 16 segs)
            // is nowhere near, but guard anyway — drop ICC instead of producing an invalid JPEG.
            output.Write(jpegBytes, 0, jpegBytes.Length);
            return;
        }

        // Write SOI.
        output.WriteByte(0xFF);
        output.WriteByte(0xD8);

        Span<byte> lenBe = stackalloc byte[2];
        for (var seg = 0; seg < totalSegments; seg++)
        {
            var srcStart = seg * MaxIccPerSegment;
            var thisLen = Math.Min(MaxIccPerSegment, icc.Length - srcStart);
            var segTotalLen = IccHeaderLen + thisLen + 2;  // +2 for the segment length field itself

            // APP2 marker
            output.WriteByte(0xFF);
            output.WriteByte(0xE2);

            BinaryPrimitives.WriteUInt16BigEndian(lenBe, (ushort)segTotalLen);
            output.Write(lenBe);

            output.Write(ColorProfile.JpegIccMarker);
            output.WriteByte((byte)(seg + 1));          // ICC seqNo is 1-based
            output.WriteByte((byte)totalSegments);
            output.Write(icc, srcStart, thisLen);
        }

        // Rest of the original JPEG, skipping the original SOI.
        output.Write(jpegBytes, 2, jpegBytes.Length - 2);
    }
}
