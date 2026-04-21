using System.Runtime.InteropServices;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// Decodes PNG input using libspng.  Supports the "already-PNG" Copy-as-PNG workflow
/// where a user right-clicks a .png file and expects clipboard-paste to just work.
/// </summary>
internal sealed class PngDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.IsEmpty) throw new DecoderException("png: empty input");

        var ctx = LibSpng.spng_ctx_new(0);
        if (ctx == IntPtr.Zero) throw new DecoderException("png: spng_ctx_new returned null");

        try
        {
            unsafe
            {
                fixed (byte* src = sourceBytes)
                {
                    var set = LibSpng.spng_set_png_buffer(ctx, (IntPtr)src, (nuint)sourceBytes.Length);
                    if (set != 0) throw new DecoderException($"png: spng_set_png_buffer returned {set}");

                    var ihdrErr = LibSpng.spng_get_ihdr(ctx, out var ihdr);
                    if (ihdrErr != 0) throw new DecoderException($"png: spng_get_ihdr returned {ihdrErr}");

                    var sizeErr = LibSpng.spng_decoded_image_size(ctx, LibSpng.SPNG_FMT_RGBA8, out var outSize);
                    if (sizeErr != 0) throw new DecoderException($"png: spng_decoded_image_size returned {sizeErr}");

                    var pixels = new byte[(int)outSize];
                    fixed (byte* dst = pixels)
                    {
                        var dec = LibSpng.spng_decode_image(ctx, (IntPtr)dst, outSize, LibSpng.SPNG_FMT_RGBA8, 0);
                        if (dec != 0) throw new DecoderException($"png: spng_decode_image returned {dec}");
                    }

                    byte[]? icc = null;
                    if (LibSpng.spng_get_iccp(ctx, out var iccp) == 0 && iccp.profile != IntPtr.Zero && iccp.profile_len > 0)
                    {
                        icc = new byte[(int)iccp.profile_len];
                        Marshal.Copy(iccp.profile, icc, 0, icc.Length);
                    }

                    return new DecodedImage
                    {
                        Width = (int)ihdr.width,
                        Height = (int)ihdr.height,
                        Pixels = pixels,
                        IccProfile = icc,
                    };
                }
            }
        }
        finally
        {
            LibSpng.spng_ctx_free(ctx);
        }
    }
}
