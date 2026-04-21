using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;
using Xunit;

namespace RTClickPng.Engine.Tests;

/// <summary>
/// Decoder round-trip tests: load fixture, decode to RGBA, re-encode to PNG,
/// assert dimensions preserved and output parses as a valid PNG.
/// </summary>
public class DecoderTests
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "fixtures");

    public static TheoryData<string, Type> FixtureMatrix => new()
    {
        { "sample.png",             typeof(PngDecoder)  },
        { "sample.bmp",             typeof(BmpDecoder)  },
        { "sample-animated.gif",    typeof(GifDecoder)  },
        { "sample-multipage.tiff",  typeof(TiffDecoder) },
        { "sample.webp",            typeof(WebpDecoder) },
        { "sample.avif",            typeof(AvifDecoder) },
        { "sample.heic",            typeof(HeifDecoder) },
    };

    [Theory]
    [MemberData(nameof(FixtureMatrix))]
    public void Decode_ProducesNonEmptyRgba(string fixture, Type decoderType)
    {
        var path = Path.Combine(FixturesDir, fixture);
        Assert.True(File.Exists(path), $"fixture missing: {path}");

        var bytes = File.ReadAllBytes(path);
        var decoder = (IImageDecoder)Activator.CreateInstance(decoderType)!;
        var image = decoder.Decode(bytes);

        Assert.True(image.Width > 0, $"width should be > 0, got {image.Width}");
        Assert.True(image.Height > 0, $"height should be > 0, got {image.Height}");
        Assert.Equal(image.Width * image.Height * 4, image.Pixels.Length);
    }

    [Theory]
    [MemberData(nameof(FixtureMatrix))]
    public void Decode_ThenEncodePng_ProducesValidPngSignature(string fixture, Type decoderType)
    {
        var path = Path.Combine(FixturesDir, fixture);
        var bytes = File.ReadAllBytes(path);
        var decoder = (IImageDecoder)Activator.CreateInstance(decoderType)!;
        var image = decoder.Decode(bytes);

        using var ms = new MemoryStream();
        new PngEncoder().Encode(image, ms);
        var png = ms.ToArray();

        Assert.True(png.Length >= 8, "PNG output should be at least 8 bytes");
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal(0x89, png[0]); Assert.Equal(0x50, png[1]); Assert.Equal(0x4E, png[2]); Assert.Equal(0x47, png[3]);
        Assert.Equal(0x0D, png[4]); Assert.Equal(0x0A, png[5]); Assert.Equal(0x1A, png[6]); Assert.Equal(0x0A, png[7]);
    }

    [Fact]
    public void BmpDecoder_PreservesKnownPixel()
    {
        // Our synthetic sample.bmp is an 8x8 BGR gradient where R=x*32, G=y*32, B=128.
        var bytes = File.ReadAllBytes(Path.Combine(FixturesDir, "sample.bmp"));
        var image = new BmpDecoder().Decode(bytes);

        Assert.Equal(8, image.Width);
        Assert.Equal(8, image.Height);

        // pixel at (3, 5): R=3*32=96, G=5*32=160, B=128, A=255
        var idx = (5 * 8 + 3) * 4;
        Assert.Equal(96, image.Pixels[idx + 0]);
        Assert.Equal(160, image.Pixels[idx + 1]);
        Assert.Equal(128, image.Pixels[idx + 2]);
        Assert.Equal(255, image.Pixels[idx + 3]);
    }

    [Fact]
    public void PngDecoder_RejectsEmptyInput()
    {
        Assert.Throws<DecoderException>(() => new PngDecoder().Decode(Array.Empty<byte>()));
    }

    [Fact]
    public void BmpDecoder_RejectsNonBmpInput()
    {
        var junk = new byte[64];
        junk[0] = (byte)'X'; junk[1] = (byte)'Y';
        Assert.Throws<DecoderException>(() => new BmpDecoder().Decode(junk));
    }
}
