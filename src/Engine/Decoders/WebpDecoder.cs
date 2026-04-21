using System.Runtime.InteropServices;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Decoders;

internal sealed class WebpDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.IsEmpty) throw new DecoderException("webp: empty input");

        unsafe
        {
            fixed (byte* src = sourceBytes)
            {
                var decoded = LibWebp.WebPDecodeRGBA((IntPtr)src, (nuint)sourceBytes.Length, out var width, out var height);
                if (decoded == IntPtr.Zero) throw new DecoderException("webp: WebPDecodeRGBA returned null");

                try
                {
                    var len = checked(width * height * 4);
                    var pixels = new byte[len];
                    Marshal.Copy(decoded, pixels, 0, len);
                    return new DecodedImage { Width = width, Height = height, Pixels = pixels };
                }
                finally
                {
                    LibWebp.WebPFree(decoded);
                }
            }
        }
    }
}
