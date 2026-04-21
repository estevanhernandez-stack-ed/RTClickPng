namespace RTClickPng.Engine.Encoders;

using RTClickPng.Engine.Decoders;

/// <summary>
/// Encodes an RGBA8 <see cref="DecodedImage"/> into a target format's byte stream.
/// Output is written to a caller-provided <see cref="Stream"/> so both file-output
/// (convert verb) and stdout-output (copy verb) share one code path.
/// </summary>
internal interface IImageEncoder
{
    /// <summary>
    /// Encode to <paramref name="output"/>.  The encoder is responsible for any
    /// format-specific color-profile chunks (iCCP for PNG, APP2 for JPEG) when
    /// <paramref name="image"/> carries an ICC profile.
    /// </summary>
    void Encode(DecodedImage image, Stream output);
}

/// <summary>
/// Thrown by any encoder when writing fails.  Callers treat as ExitCode.OutputFailed (=5).
/// </summary>
internal sealed class EncoderException(string message) : Exception(message);
