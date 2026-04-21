namespace RTClickPng.Engine.Color;

/// <summary>
/// Shared helpers for handling ICC color profiles across decoders and encoders.
/// Decoders populate <see cref="Decoders.DecodedImage.IccProfile"/> when the source has one;
/// encoders consult that field to write out the equivalent format-specific chunk
/// (iCCP for PNG, APP2 "ICC_PROFILE" for JPEG).
/// </summary>
internal static class ColorProfile
{
    /// <summary>
    /// Maximum size of an ICC profile we're willing to pass through, in bytes.
    /// ICC v4 profiles commonly run 1-20 KB but can balloon into the MB range for exotic devices.
    /// We cap at 1 MB to avoid pathological inputs blowing our output size.
    /// </summary>
    internal const int MaxProfileSize = 1_048_576;

    /// <summary>
    /// Returns <paramref name="profile"/> if it's non-null and within size bounds, else null.
    /// </summary>
    public static byte[]? Sanitize(byte[]? profile)
    {
        if (profile is null || profile.Length == 0 || profile.Length > MaxProfileSize) return null;
        return profile;
    }

    /// <summary>
    /// "ICC_PROFILE\0" marker string used by JPEG APP2 and some other container formats.
    /// </summary>
    public static ReadOnlySpan<byte> JpegIccMarker => "ICC_PROFILE\0"u8;
}
