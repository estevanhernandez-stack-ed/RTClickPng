using System.Runtime.InteropServices;
using RTClickPng.Engine.Interop;

namespace RTClickPng.Engine.Decoders;

/// <summary>
/// AVIF decoder via libavif.  Uses the avifDecoderReadMemory path which gives us a caller-owned
/// avifImage directly and avoids dereferencing decoder internals — much more stable across libavif
/// minor versions than reading <c>decoder-&gt;image</c> at a fixed offset.
/// </summary>
internal sealed class AvifDecoder : IImageDecoder
{
    public unsafe DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
    {
        if (sourceBytes.IsEmpty) throw new DecoderException("avif: empty input");

        var decoder = LibAvif.avifDecoderCreate();
        if (decoder == IntPtr.Zero) throw new DecoderException("avif: avifDecoderCreate failed");

        var image = LibAvif.avifImageCreateEmpty();
        if (image == IntPtr.Zero)
        {
            LibAvif.avifDecoderDestroy(decoder);
            throw new DecoderException("avif: avifImageCreateEmpty failed");
        }

        try
        {
            fixed (byte* src = sourceBytes)
            {
                var rc = LibAvif.avifDecoderReadMemory(decoder, image, (IntPtr)src, (nuint)sourceBytes.Length);
                if (rc != LibAvif.AVIF_RESULT_OK)
                    throw new DecoderException($"avif: avifDecoderReadMemory returned {rc}");

                // width/height/depth live at the head of avifImage.
                var headPtr = (uint*)image;
                var imgW = (int)headPtr[0];
                var imgH = (int)headPtr[1];
                if (imgW <= 0 || imgH <= 0)
                    throw new DecoderException($"avif: zero-sized image ({imgW}x{imgH})");

                var rgb = default(LibAvif.avifRGBImage);
                LibAvif.avifRGBImageSetDefaults(ref rgb, image);
                rgb.format = LibAvif.AVIF_RGB_FORMAT_RGBA;
                rgb.depth = 8;   // tone-map to 8-bit per channel regardless of source (PRD)

                var alloc = LibAvif.avifRGBImageAllocatePixels(ref rgb);
                if (alloc != LibAvif.AVIF_RESULT_OK)
                    throw new DecoderException($"avif: avifRGBImageAllocatePixels returned {alloc}");

                try
                {
                    var conv = LibAvif.avifImageYUVToRGB(image, ref rgb);
                    if (conv != LibAvif.AVIF_RESULT_OK)
                        throw new DecoderException($"avif: avifImageYUVToRGB returned {conv}");

                    if (rgb.pixels == IntPtr.Zero)
                        throw new DecoderException("avif: RGB pixels pointer is null after conversion");

                    var width = (int)rgb.width;
                    var height = (int)rgb.height;
                    var rowBytes = (int)rgb.rowBytes;
                    var pixels = new byte[width * height * 4];
                    var dstRow = 0;
                    for (var y = 0; y < height; y++)
                    {
                        Marshal.Copy(rgb.pixels + y * rowBytes, pixels, dstRow, width * 4);
                        dstRow += width * 4;
                    }
                    return new DecodedImage { Width = width, Height = height, Pixels = pixels };
                }
                finally
                {
                    LibAvif.avifRGBImageFreePixels(ref rgb);
                }
            }
        }
        finally
        {
            LibAvif.avifImageDestroy(image);
            LibAvif.avifDecoderDestroy(decoder);
        }
    }
}
