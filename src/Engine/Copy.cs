using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;
using RTClickPng.Engine.Metadata;

namespace RTClickPng.Engine;

/// <summary>
/// Handles the <c>copy &lt;src&gt;</c> verb — decodes the source and writes PNG bytes to stdout
/// with a 4-byte big-endian length prefix, so the Shell Extension can safely bound the pipe read.
/// Applies EXIF orientation and strips all other EXIF before encoding.
/// </summary>
internal static class CopyCommand
{
    public static int Run(ReadOnlySpan<string> args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("copy: expected <source>");
            return (int)ExitCode.Generic;
        }

        var source = args[0];
        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"copy: source not found: {source}");
            return (int)ExitCode.SourceNotFound;
        }

        try
        {
            var srcBytes = File.ReadAllBytes(source);
            var srcExt = Path.GetExtension(source);

            IImageDecoder decoder;
            try { decoder = FormatRegistry.DecoderForExtension(srcExt); }
            catch (DecoderException ex)
            {
                Console.Error.WriteLine($"copy: {ex.Message}");
                return (int)ExitCode.FormatUnsupported;
            }

            var image = decoder.Decode(srcBytes);

            if (image.Exif is { Length: > 0 })
            {
                var orientation = ExifHandler.ReadOrientation(image.Exif);
                if (orientation != ExifHandler.Orientation.Normal)
                {
                    var (rotated, newW, newH) = ExifHandler.ApplyOrientation(image.Pixels, image.Width, image.Height, orientation);
                    image = new DecodedImage
                    {
                        Width = newW, Height = newH, Pixels = rotated,
                        IccProfile = image.IccProfile, Exif = null,
                    };
                }
            }

            using var mem = new MemoryStream();
            new PngEncoder().Encode(image, mem);
            mem.Flush();
            var png = mem.ToArray();

            using var stdout = Console.OpenStandardOutput();
            Span<byte> len = stackalloc byte[4];
            len[0] = (byte)((png.Length >> 24) & 0xFF);
            len[1] = (byte)((png.Length >> 16) & 0xFF);
            len[2] = (byte)((png.Length >>  8) & 0xFF);
            len[3] = (byte)( png.Length        & 0xFF);
            stdout.Write(len);
            stdout.Write(png, 0, png.Length);
            return (int)ExitCode.Success;
        }
        catch (DecoderException ex) { Console.Error.WriteLine($"copy: {ex.Message}"); return (int)ExitCode.FormatUnsupported; }
        catch (EncoderException ex) { Console.Error.WriteLine($"copy: {ex.Message}"); return (int)ExitCode.OutputFailed; }
        catch (IOException ex)       { Console.Error.WriteLine($"copy: {ex.Message}"); return (int)ExitCode.OutputFailed; }
    }
}
