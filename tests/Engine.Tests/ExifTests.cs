using RTClickPng.Engine.Metadata;
using Xunit;

namespace RTClickPng.Engine.Tests;

public class ExifTests
{
    /// <summary>
    /// Build a minimal little-endian TIFF/EXIF stream with a single Orientation tag (0x0112).
    /// </summary>
    private static byte[] MakeExifWithOrientation(ushort orientation)
    {
        // Header: II 42 08 (ifd0 at offset 8)
        // IFD: entries=1, tag=0x0112 type=3(SHORT) count=1 value=orientation, next=0
        var buf = new byte[8 + 2 + 12 + 4];
        buf[0] = (byte)'I'; buf[1] = (byte)'I';
        buf[2] = 42; buf[3] = 0;
        buf[4] = 8; buf[5] = 0; buf[6] = 0; buf[7] = 0;
        buf[8] = 1; buf[9] = 0;                // 1 entry
        buf[10] = 0x12; buf[11] = 0x01;       // tag 0x0112
        buf[12] = 3; buf[13] = 0;              // type SHORT
        buf[14] = 1; buf[15] = 0; buf[16] = 0; buf[17] = 0;   // count=1
        buf[18] = (byte)orientation; buf[19] = 0;             // value
        // remaining 4 bytes are padding/nextIFD (all zero)
        return buf;
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)2)]
    [InlineData((ushort)3)]
    [InlineData((ushort)4)]
    [InlineData((ushort)5)]
    [InlineData((ushort)6)]
    [InlineData((ushort)7)]
    [InlineData((ushort)8)]
    public void ReadOrientation_ParsesAllStandardValues(ushort raw)
    {
        // InlineData can't carry internal enum values directly; compare the integer underlying form.
        var exif = MakeExifWithOrientation(raw);
        var actual = ExifHandler.ReadOrientation(exif);
        Assert.Equal(raw, (ushort)actual);
    }

    [Fact]
    public void ReadOrientation_DefaultsToNormalOnGarbage()
    {
        Assert.Equal(ExifHandler.Orientation.Normal, ExifHandler.ReadOrientation(Array.Empty<byte>()));
        Assert.Equal(ExifHandler.Orientation.Normal, ExifHandler.ReadOrientation(new byte[] { 0, 1, 2, 3 }));
        Assert.Equal(ExifHandler.Orientation.Normal, ExifHandler.ReadOrientation([.. "ZZ"u8, 42, 0, 8, 0, 0, 0]));
    }

    [Fact]
    public void ApplyOrientation_Rotate90Cw_SwapsDimensionsAndMovesTopLeftToTopRight()
    {
        // 3x2 source: pixel coords (x,y) with unique marker.
        // [0,0]=R [1,0]=G [2,0]=B
        // [0,1]=Y [1,1]=M [2,1]=C
        var w = 3; var h = 2;
        var src = new byte[w * h * 4];
        void Put(int x, int y, byte r, byte g, byte b) { var i = (y * w + x) * 4; src[i] = r; src[i + 1] = g; src[i + 2] = b; src[i + 3] = 255; }
        Put(0, 0, 255, 0,   0);   // R top-left
        Put(1, 0, 0,   255, 0);   // G
        Put(2, 0, 0,   0,   255); // B top-right
        Put(0, 1, 255, 255, 0);   // Y bottom-left
        Put(1, 1, 255, 0,   255); // M
        Put(2, 1, 0,   255, 255); // C bottom-right

        var (dst, newW, newH) = ExifHandler.ApplyOrientation(src, w, h, ExifHandler.Orientation.Rotate90Cw);
        Assert.Equal(h, newW); Assert.Equal(w, newH);

        // After rotate-90-cw, the old top-left (R) is now at the top-right.
        // New dimensions: newW=2, newH=3. Top-right = (newW-1, 0) = (1, 0).
        var topRightIdx = (0 * newW + (newW - 1)) * 4;
        Assert.Equal(255, dst[topRightIdx + 0]);
        Assert.Equal(0,   dst[topRightIdx + 1]);
        Assert.Equal(0,   dst[topRightIdx + 2]);
    }

    [Fact]
    public void ApplyOrientation_Normal_IsIdentity()
    {
        var pixels = new byte[16 * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = (byte)i;
        var (dst, w, h) = ExifHandler.ApplyOrientation(pixels, 4, 4, ExifHandler.Orientation.Normal);
        Assert.Same(pixels, dst);
        Assert.Equal(4, w);
        Assert.Equal(4, h);
    }
}
