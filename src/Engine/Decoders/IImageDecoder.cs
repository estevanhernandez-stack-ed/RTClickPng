namespace RTClickPng.Engine.Decoders;

/// <summary>
/// Decodes compressed image bytes into a uniform RGBA8 bitmap plus optional ICC profile.
/// One implementation per input format.  Decoders are stateless singletons — the engine
/// creates one per verb invocation.
/// </summary>
internal interface IImageDecoder
{
    /// <summary>
    /// Decode a complete file into pixels.
    /// </summary>
    /// <param name="sourceBytes">The entire source file contents (we always read whole files — sizes are bounded by PRD §3).</param>
    /// <returns>A <see cref="DecodedImage"/> with RGBA8 pixels and any ICC profile extracted from the container.</returns>
    /// <exception cref="DecoderException">Thrown for corrupt input, unsupported sub-formats, or native library failures.</exception>
    DecodedImage Decode(ReadOnlySpan<byte> sourceBytes);
}

/// <summary>
/// An in-memory decoded image.  RGBA8, straight (non-premultiplied) alpha, sRGB unless <see cref="IccProfile"/> says otherwise.
/// </summary>
internal sealed class DecodedImage
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    /// <summary>Row-major, tightly packed, 4 bytes per pixel (R,G,B,A).  Length == Width*Height*4.</summary>
    public required byte[] Pixels { get; init; }
    /// <summary>Raw ICC profile bytes if the source had an embedded profile, otherwise null.</summary>
    public byte[]? IccProfile { get; init; }
    /// <summary>Raw EXIF bytes (APP1 segment contents) if present.  Parsed lazily by <c>ExifHandler</c> (item 4).</summary>
    public byte[]? Exif { get; init; }
}

/// <summary>
/// Thrown by any decoder when input cannot be interpreted.  Callers treat as ExitCode.FormatUnsupported (=3).
/// </summary>
internal sealed class DecoderException(string message) : Exception(message);
