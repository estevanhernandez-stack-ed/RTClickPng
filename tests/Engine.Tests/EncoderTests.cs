using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;
using Xunit;

namespace RTClickPng.Engine.Tests;

/// <summary>
/// Encoder smoke tests: synthesize an RGBA bitmap, encode via PngEncoder / JpegEncoder,
/// then decode the encoded bytes back via PngDecoder and confirm dimensions + signature.
/// </summary>
public class EncoderTests
{
    private static DecodedImage SynthGradient(int w, int h)
    {
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                pixels[i + 0] = (byte)(x * 255 / w);
                pixels[i + 1] = (byte)(y * 255 / h);
                pixels[i + 2] = 64;
                pixels[i + 3] = 255;
            }
        return new DecodedImage { Width = w, Height = h, Pixels = pixels };
    }

    [Fact]
    public void PngEncoder_RoundTripsDimensions()
    {
        var src = SynthGradient(32, 24);
        using var ms = new MemoryStream();
        new PngEncoder().Encode(src, ms);
        var encoded = ms.ToArray();

        var decoded = new PngDecoder().Decode(encoded);
        Assert.Equal(src.Width, decoded.Width);
        Assert.Equal(src.Height, decoded.Height);
        Assert.Equal(src.Pixels.Length, decoded.Pixels.Length);
    }

    [Fact]
    public void PngEncoder_PreservesPixelData()
    {
        var src = SynthGradient(16, 16);
        using var ms = new MemoryStream();
        new PngEncoder().Encode(src, ms);
        var decoded = new PngDecoder().Decode(ms.ToArray());

        // Spot-check a few pixels to confirm no data corruption.
        for (var i = 0; i < src.Pixels.Length; i += 137) // stride chosen to hit diverse offsets in a 16x16 buffer
        {
            Assert.Equal(src.Pixels[i], decoded.Pixels[i]);
        }
    }

    [Fact]
    public void JpegEncoder_ProducesValidJpegSignature()
    {
        var src = SynthGradient(64, 64);
        using var ms = new MemoryStream();
        new JpegEncoder().Encode(src, ms);
        var encoded = ms.ToArray();

        Assert.True(encoded.Length > 2, "JPEG output must be at least 3 bytes");
        // JPEG SOI marker: FF D8
        Assert.Equal(0xFF, encoded[0]);
        Assert.Equal(0xD8, encoded[1]);
        // Ends with EOI FF D9
        Assert.Equal(0xFF, encoded[^2]);
        Assert.Equal(0xD9, encoded[^1]);
    }

    [Fact]
    public void PngEncoder_EmbedsIccProfileAsIccpChunk()
    {
        var src = SynthGradient(8, 8);
        var iccBlob = new byte[32];   // synthetic non-empty ICC
        for (var i = 0; i < iccBlob.Length; i++) iccBlob[i] = (byte)(i * 7);

        var withIcc = new DecodedImage
        {
            Width = src.Width,
            Height = src.Height,
            Pixels = src.Pixels,
            IccProfile = iccBlob,
        };
        using var ms = new MemoryStream();
        new PngEncoder().Encode(withIcc, ms);
        var encoded = ms.ToArray();

        // Scan for "iCCP" chunk type in output.
        var found = false;
        for (var i = 8; i + 4 <= encoded.Length; i++)
        {
            if (encoded[i] == (byte)'i' && encoded[i + 1] == (byte)'C' &&
                encoded[i + 2] == (byte)'C' && encoded[i + 3] == (byte)'P')
            {
                found = true; break;
            }
        }
        Assert.True(found, "expected iCCP chunk in PNG output when IccProfile is set");
    }
}
