using System.Runtime.InteropServices;
using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Encoders;

/// <summary>
/// PNG encoder via libspng.  Writes 8-bit RGBA PNG with iCCP chunk when <see cref="DecodedImage.IccProfile"/> is set.
/// </summary>
internal sealed class PngEncoder : IImageEncoder
{
    public void Encode(DecodedImage image, Stream output)
    {
        var ctx = LibSpng.spng_ctx_new(LibSpng.SPNG_CTX_ENCODER);
        if (ctx == IntPtr.Zero) throw new EncoderException("png: spng_ctx_new returned null");

        try
        {
            var r = LibSpng.spng_set_option(ctx, LibSpng.SPNG_ENCODE_TO_BUFFER, 1);
            if (r != 0) throw new EncoderException($"png: spng_set_option(SPNG_ENCODE_TO_BUFFER) returned {r}");

            var ihdr = new LibSpng.spng_ihdr
            {
                width = (uint)image.Width,
                height = (uint)image.Height,
                bit_depth = 8,
                color_type = LibSpng.SPNG_COLOR_TYPE_TRUECOLOR_ALPHA,
                compression_method = 0,
                filter_method = 0,
                interlace_method = 0,
            };
            r = LibSpng.spng_set_ihdr(ctx, ref ihdr);
            if (r != 0) throw new EncoderException($"png: spng_set_ihdr returned {r}");

            unsafe
            {
                if (image.IccProfile is { Length: > 0 })
                {
                    fixed (byte* profilePtr = image.IccProfile)
                    {
                        var iccp = default(LibSpng.spng_iccp);
                        // "ICC profile" in ASCII — libspng's spng_iccp name field is a 80-byte array at struct offset 0.
                        var name = "ICC profile"u8;
                        var iccpPtr = (byte*)&iccp;
                        for (var i = 0; i < name.Length; i++) iccpPtr[i] = name[i];
                        iccp.profile_len = (nuint)image.IccProfile.Length;
                        iccp.profile = (IntPtr)profilePtr;
                        r = LibSpng.spng_set_iccp(ctx, ref iccp);
                        if (r != 0) throw new EncoderException($"png: spng_set_iccp returned {r}");
                    }
                }

                fixed (byte* pixelsPtr = image.Pixels)
                {
                    // SPNG_FMT_PNG = "my input data is already laid out per the IHDR I provided".
                    // Our IHDR is RGBA8 (color_type=6, bit_depth=8), which matches our buffer, so PNG format is the right choice.
                    r = LibSpng.spng_encode_image(ctx, (IntPtr)pixelsPtr, (nuint)image.Pixels.Length,
                                                  LibSpng.SPNG_FMT_PNG, LibSpng.SPNG_ENCODE_FINALIZE);
                    if (r != 0) throw new EncoderException($"png: spng_encode_image returned {r}");
                }
            }

            var buf = LibSpng.spng_get_png_buffer(ctx, out var size, out var err);
            if (err != 0 || buf == IntPtr.Zero) throw new EncoderException($"png: spng_get_png_buffer returned err={err}");
            try
            {
                var managed = new byte[(int)size];
                Marshal.Copy(buf, managed, 0, managed.Length);
                output.Write(managed, 0, managed.Length);
            }
            finally
            {
                // Buffer is allocated via stdlib free — C runtime free is callable from managed code
                // via Marshal.FreeHGlobal since libspng uses the CRT's free.  If this becomes an issue
                // we add a libspng-provided free helper.  v1 leaks nothing in practice because the
                // Engine process exits after each invocation.
                Marshal.FreeHGlobal(buf);
            }
        }
        finally
        {
            LibSpng.spng_ctx_free(ctx);
        }
    }
}
