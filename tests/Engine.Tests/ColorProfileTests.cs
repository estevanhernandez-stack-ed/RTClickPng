using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;
using Xunit;

namespace RTClickPng.Engine.Tests;

/// <summary>
/// ICC round-trip tests.  Verifies that a DecodedImage carrying an ICC profile survives
/// both PNG (iCCP chunk) and JPEG (APP2 ICC_PROFILE) encodes, and can be read back from PNG.
/// </summary>
public class ColorProfileTests
{
    /// <summary>Build a dummy ICC blob large enough to look valid (ICC headers are 128 bytes).</summary>
    private static byte[] SynthIcc()
    {
        var icc = new byte[256];
        // Byte 0-3 = profile size (big-endian 256)
        icc[2] = 0x01; icc[3] = 0x00;
        // Fill rest with pseudorandom but deterministic content
        for (var i = 4; i < icc.Length; i++) icc[i] = (byte)(i * 13 % 256);
        return icc;
    }

    private static DecodedImage MakeSynthImageWithIcc(int w, int h, byte[] icc)
    {
        var pixels = new byte[w * h * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)i;
        return new DecodedImage { Width = w, Height = h, Pixels = pixels, IccProfile = icc };
    }

    [Fact]
    public void PngEncoder_EmbedsIccProfile_AsIccpChunk()
    {
        var image = MakeSynthImageWithIcc(8, 8, SynthIcc());
        using var ms = new MemoryStream();
        new PngEncoder().Encode(image, ms);
        var encoded = ms.ToArray();
        Assert.True(FindAscii(encoded, "iCCP"u8), "expected iCCP chunk in PNG output");
    }

    [Fact]
    public void PngEncoder_IccProfile_RoundTripsThroughPngDecoder()
    {
        var icc = SynthIcc();
        var image = MakeSynthImageWithIcc(8, 8, icc);
        using var ms = new MemoryStream();
        new PngEncoder().Encode(image, ms);

        var decoded = new PngDecoder().Decode(ms.ToArray());
        Assert.NotNull(decoded.IccProfile);
        Assert.Equal(icc.Length, decoded.IccProfile!.Length);
        for (var i = 0; i < icc.Length; i++) Assert.Equal(icc[i], decoded.IccProfile[i]);
    }

    [Fact]
    public void JpegEncoder_EmbedsIccProfile_AsApp2Marker()
    {
        var image = MakeSynthImageWithIcc(32, 32, SynthIcc());
        using var ms = new MemoryStream();
        new JpegEncoder().Encode(image, ms);
        var encoded = ms.ToArray();
        Assert.True(FindAscii(encoded, "ICC_PROFILE\0"u8), "expected ICC_PROFILE marker in JPEG output");

        // Also confirm JPEG structure: starts with SOI, has at least one APP2 marker (FF E2) after SOI.
        Assert.Equal(0xFF, encoded[0]);
        Assert.Equal(0xD8, encoded[1]);
        Assert.Equal(0xFF, encoded[2]);
        Assert.Equal(0xE2, encoded[3]);
    }

    [Fact]
    public void JpegEncoder_WithoutIcc_HasNoApp2Marker()
    {
        var pixels = new byte[8 * 8 * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)(i * 3);
        var image = new DecodedImage { Width = 8, Height = 8, Pixels = pixels, IccProfile = null };

        using var ms = new MemoryStream();
        new JpegEncoder().Encode(image, ms);
        Assert.False(FindAscii(ms.ToArray(), "ICC_PROFILE\0"u8), "no ICC marker expected when IccProfile is null");
    }

    private static bool FindAscii(byte[] haystack, ReadOnlySpan<byte> needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle)) return true;
        }
        return false;
    }
}
