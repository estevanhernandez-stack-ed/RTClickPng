using System.Buffers.Binary;

namespace RTClickPng.Engine.Metadata;

/// <summary>
/// Lightweight EXIF parser focused on tag 0x0112 (Orientation).  We only care about orientation
/// for this product — everything else in EXIF (GPS, timestamps, serial numbers, ...) is privacy-
/// sensitive metadata the user almost certainly doesn't want leaking through a context-menu
/// conversion.  So we read orientation, apply it to the RGBA buffer, and discard the rest.
/// </summary>
internal static class ExifHandler
{
    /// <summary>
    /// TIFF orientation tag values.  See EXIF 2.32 §4.6.4 for the canonical reference.
    /// </summary>
    internal enum Orientation : ushort
    {
        Normal = 1,          // row 0 = top, col 0 = left
        FlipHorizontal = 2,  // row 0 = top, col 0 = right
        Rotate180 = 3,       // row 0 = bottom, col 0 = right
        FlipVertical = 4,    // row 0 = bottom, col 0 = left
        Transpose = 5,       // row 0 = left, col 0 = top (flip diagonal)
        Rotate90Cw = 6,      // row 0 = right, col 0 = top
        Transverse = 7,      // row 0 = right, col 0 = bottom (flip anti-diagonal)
        Rotate90Ccw = 8,     // row 0 = left, col 0 = bottom
    }

    /// <summary>
    /// Parse TIFF-header EXIF bytes and return the orientation tag value, or Normal if missing.
    /// <paramref name="exif"/> is the raw TIFF stream starting with "II" or "MM" magic — i.e. the
    /// bytes after the "Exif\0\0" marker in a JPEG APP1 segment, or the container-provided EXIF blob
    /// from HEIC/AVIF.
    /// </summary>
    public static Orientation ReadOrientation(ReadOnlySpan<byte> exif)
    {
        if (exif.Length < 8) return Orientation.Normal;

        bool little;
        if (exif[0] == 'I' && exif[1] == 'I') little = true;
        else if (exif[0] == 'M' && exif[1] == 'M') little = false;
        else return Orientation.Normal;

        var magic = ReadU16(exif[2..4], little);
        if (magic != 42) return Orientation.Normal;

        var ifd0 = ReadU32(exif[4..8], little);
        if (ifd0 + 2 > (uint)exif.Length) return Orientation.Normal;

        int entries = ReadU16(exif.Slice((int)ifd0, 2), little);
        for (var i = 0; i < entries; i++)
        {
            var ofs = (int)ifd0 + 2 + i * 12;
            if (ofs + 12 > exif.Length) break;
            var tag = ReadU16(exif.Slice(ofs, 2), little);
            if (tag != 0x0112) continue;
            var type = ReadU16(exif.Slice(ofs + 2, 2), little);
            if (type != 3) return Orientation.Normal;  // expect SHORT
            var value = ReadU16(exif.Slice(ofs + 8, 2), little);
            if (value is >= 1 and <= 8) return (Orientation)value;
            return Orientation.Normal;
        }
        return Orientation.Normal;
    }

    /// <summary>
    /// Apply an EXIF orientation to an RGBA8 bitmap, returning a new bitmap with the transform
    /// pre-baked so downstream encoders see the "correct" pixel orientation with no surviving EXIF.
    /// </summary>
    public static (byte[] Pixels, int Width, int Height) ApplyOrientation(
        byte[] src, int width, int height, Orientation orientation)
    {
        if (orientation == Orientation.Normal) return (src, width, height);

        // Rotations swap dimensions.
        var (newW, newH) = orientation switch
        {
            Orientation.Rotate90Cw or Orientation.Rotate90Ccw or Orientation.Transpose or Orientation.Transverse
                => (height, width),
            _ => (width, height),
        };

        var dst = new byte[newW * newH * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (dx, dy) = orientation switch
                {
                    Orientation.FlipHorizontal => (width - 1 - x,     y),
                    Orientation.Rotate180      => (width - 1 - x,     height - 1 - y),
                    Orientation.FlipVertical   => (x,                 height - 1 - y),
                    Orientation.Transpose      => (y,                 x),
                    Orientation.Rotate90Cw     => (height - 1 - y,    x),
                    Orientation.Transverse     => (height - 1 - y,    width - 1 - x),
                    Orientation.Rotate90Ccw    => (y,                 width - 1 - x),
                    _ => (x, y),
                };
                var srcIdx = (y * width + x) * 4;
                var dstIdx = (dy * newW + dx) * 4;
                dst[dstIdx + 0] = src[srcIdx + 0];
                dst[dstIdx + 1] = src[srcIdx + 1];
                dst[dstIdx + 2] = src[srcIdx + 2];
                dst[dstIdx + 3] = src[srcIdx + 3];
            }
        }
        return (dst, newW, newH);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> s, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(s) : BinaryPrimitives.ReadUInt16BigEndian(s);
    private static uint ReadU32(ReadOnlySpan<byte> s, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(s) : BinaryPrimitives.ReadUInt32BigEndian(s);
}
