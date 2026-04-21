using System.Runtime.InteropServices;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Decoders;

internal sealed class HeifDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.IsEmpty) throw new DecoderException("heif: empty input");

        var ctx = LibHeif.heif_context_alloc();
        if (ctx == IntPtr.Zero) throw new DecoderException("heif: heif_context_alloc returned null");

        try
        {
            unsafe
            {
                fixed (byte* src = sourceBytes)
                {
                    var err = LibHeif.heif_context_read_from_memory_without_copy(
                        ctx, (IntPtr)src, (nuint)sourceBytes.Length, IntPtr.Zero);
                    ThrowOnError(err, "heif_context_read_from_memory_without_copy");

                    err = LibHeif.heif_context_get_primary_image_handle(ctx, out var handle);
                    ThrowOnError(err, "heif_context_get_primary_image_handle");

                    try
                    {
                        err = LibHeif.heif_decode_image(
                            handle, out var img,
                            LibHeif.HEIF_COLORSPACE_RGB, LibHeif.HEIF_CHROMA_INTERLEAVED_RGBA,
                            IntPtr.Zero);
                        ThrowOnError(err, "heif_decode_image");

                        try
                        {
                            var width = LibHeif.heif_image_get_width(img, LibHeif.HEIF_CHANNEL_INTERLEAVED);
                            var height = LibHeif.heif_image_get_height(img, LibHeif.HEIF_CHANNEL_INTERLEAVED);
                            var plane = LibHeif.heif_image_get_plane_readonly(img, LibHeif.HEIF_CHANNEL_INTERLEAVED, out var stride);
                            if (plane == IntPtr.Zero) throw new DecoderException("heif: empty pixel plane");

                            var pixels = new byte[width * height * 4];
                            var dstRow = 0;
                            for (var y = 0; y < height; y++)
                            {
                                Marshal.Copy(plane + y * stride, pixels, dstRow, width * 4);
                                dstRow += width * 4;
                            }
                            return new DecodedImage { Width = width, Height = height, Pixels = pixels };
                        }
                        finally
                        {
                            LibHeif.heif_image_release(img);
                        }
                    }
                    finally
                    {
                        LibHeif.heif_image_handle_release(handle);
                    }
                }
            }
        }
        finally
        {
            LibHeif.heif_context_free(ctx);
        }
    }

    private static void ThrowOnError(LibHeif.heif_error err, string op)
    {
        if (err.code == 0) return;
        var msg = err.message != IntPtr.Zero ? Marshal.PtrToStringAnsi(err.message) : null;
        throw new DecoderException($"heif: {op} failed (code={err.code} subcode={err.subcode}): {msg}");
    }
}
