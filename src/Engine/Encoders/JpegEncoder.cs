using System.Runtime.InteropServices;
using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Encoders;

/// <summary>
/// JPEG encoder via libjpeg-turbo (TurboJPEG API).  Writes quality-92 4:2:0 JPEG.
/// ICC profile embedding via APP2 segment arrives in item 4 — for now, ICC is discarded silently.
/// Alpha is discarded (JPEG has no alpha); transparent pixels are composited against white.
/// </summary>
internal sealed class JpegEncoder : IImageEncoder
{
    public void Encode(DecodedImage image, Stream output)
    {
        // TurboJPEG consumes RGB (or RGBA), but 4:2:0 encode with RGBA source still works —
        // alpha is ignored. We flatten against white first to avoid surprise transparency-to-black.
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
                // over white: out = src.rgb * alpha + 255 * (1 - alpha)
                var aF = a / 255.0;
                flattened[i + 0] = (byte)(rgba[i + 0] * aF + 255 * (1 - aF));
                flattened[i + 1] = (byte)(rgba[i + 1] * aF + 255 * (1 - aF));
                flattened[i + 2] = (byte)(rgba[i + 2] * aF + 255 * (1 - aF));
            }
            flattened[i + 3] = 255;
        }

        var handle = LibJpegTurbo.tjInitCompress();
        if (handle == IntPtr.Zero) throw new EncoderException("jpeg: tjInitCompress returned null");

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
                        92,  // quality
                        0);
                }
            }

            if (rc != 0 || jpegBuf == IntPtr.Zero)
                throw new EncoderException($"jpeg: tjCompress2 returned {rc}");

            try
            {
                var managed = new byte[(int)jpegSize];
                Marshal.Copy(jpegBuf, managed, 0, managed.Length);
                output.Write(managed, 0, managed.Length);
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
    }
}
