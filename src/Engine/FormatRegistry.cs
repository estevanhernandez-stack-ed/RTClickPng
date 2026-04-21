using RTClickPng.Engine.Decoders;

namespace RTClickPng.Engine;

/// <summary>
/// Maps a source file extension to the decoder that handles it.  Extension-based rather than
/// magic-sniffing because the Shell Extension has already filtered by extension allowlist
/// (<c>FileFilter</c> in the C++ layer) — doing it again here is redundant overhead.
/// </summary>
internal static class FormatRegistry
{
    public static IImageDecoder DecoderForExtension(string extension)
    {
        var e = extension.ToLowerInvariant();
        if (e.StartsWith('.')) e = e[1..];
        return e switch
        {
            "webp" => new WebpDecoder(),
            "avif" => new AvifDecoder(),
            "heic" or "heif" => new HeifDecoder(),
            "jpg" or "jpeg" => new JpegInputDecoder(),   // implemented in item 4 when we add EXIF — stub below keeps the switch total
            "png"  => new PngDecoder(),
            "bmp"  => new BmpDecoder(),
            "tif" or "tiff" => new TiffDecoder(),
            "gif"  => new GifDecoder(),
            _ => throw new DecoderException($"unsupported source extension '{extension}'"),
        };
    }
}

/// <summary>
/// Placeholder JPEG decoder that composes libjpeg-turbo's decompress path.  Full implementation
/// lands in item 4 alongside the EXIF orientation work; for item 3 we keep it build-clean.
/// </summary>
internal sealed class JpegInputDecoder : IImageDecoder
{
    public DecodedImage Decode(ReadOnlySpan<byte> sourceBytes)
        => throw new DecoderException("jpeg source decoding lands in checklist item 4 (ExifHandler pairs with this)");
}
