using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;
using RTClickPng.Engine.Metadata;

namespace RTClickPng.Engine;

/// <summary>
/// Handles the <c>convert &lt;src&gt; &lt;dst&gt; [--overwrite-policy=...]</c> verb.
/// Flow: resolve policy → validate source → read → decode → apply EXIF orientation → encode → write.
/// </summary>
internal static class ConvertCommand
{
    public static int Run(ReadOnlySpan<string> args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("convert: expected <source> <destination>");
            return (int)ExitCode.Generic;
        }

        var source = args[0];
        var destination = args[1];
        var policy = OverwritePolicy.Confirm;

        for (var i = 2; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--overwrite-policy=", StringComparison.Ordinal))
            {
                policy = arg["--overwrite-policy=".Length..] switch
                {
                    "confirm" => OverwritePolicy.Confirm,
                    "force"   => OverwritePolicy.Force,
                    "skip"    => OverwritePolicy.Skip,
                    _         => OverwritePolicy.Confirm,
                };
            }
            else
            {
                Console.Error.WriteLine($"convert: unknown flag '{arg}'");
                return (int)ExitCode.Generic;
            }
        }

        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"convert: source not found: {source}");
            return (int)ExitCode.SourceNotFound;
        }

        // Overwrite enforcement.  Engine is non-interactive: the Shell Extension owns the prompt
        // and signals the decision by passing --overwrite-policy=force or =skip on retry.
        if (File.Exists(destination))
        {
            switch (policy)
            {
                case OverwritePolicy.Confirm:
                case OverwritePolicy.Skip:
                    Console.Error.WriteLine($"convert: destination exists (policy={policy.ToString().ToLowerInvariant()}): {destination}");
                    return (int)ExitCode.OverwriteDenied;
                case OverwritePolicy.Force:
                    break;
            }
        }

        try
        {
            var srcBytes = File.ReadAllBytes(source);
            var srcExt = Path.GetExtension(source);
            var dstExt = Path.GetExtension(destination);

            IImageDecoder decoder;
            try { decoder = FormatRegistry.DecoderForExtension(srcExt); }
            catch (DecoderException ex)
            {
                Console.Error.WriteLine($"convert: {ex.Message}");
                return (int)ExitCode.FormatUnsupported;
            }

            var image = decoder.Decode(srcBytes);

            // Apply EXIF orientation to the raw pixels; discard everything else in EXIF
            // (privacy-sensitive — GPS, serial numbers, timestamps — doesn't belong in the output).
            if (image.Exif is { Length: > 0 })
            {
                var orientation = ExifHandler.ReadOrientation(image.Exif);
                if (orientation != ExifHandler.Orientation.Normal)
                {
                    var (rotated, newW, newH) = ExifHandler.ApplyOrientation(image.Pixels, image.Width, image.Height, orientation);
                    image = new DecodedImage
                    {
                        Width = newW,
                        Height = newH,
                        Pixels = rotated,
                        IccProfile = image.IccProfile,
                        Exif = null,  // stripped
                    };
                }
                else
                {
                    // Orientation is already normal — just strip the EXIF payload for privacy.
                    image = new DecodedImage
                    {
                        Width = image.Width, Height = image.Height, Pixels = image.Pixels,
                        IccProfile = image.IccProfile, Exif = null,
                    };
                }
            }

            IImageEncoder encoder = dstExt.ToLowerInvariant() switch
            {
                ".png" => new PngEncoder(),
                ".jpg" or ".jpeg" => new JpegEncoder(),
                _ => throw new EncoderException($"convert: unsupported destination extension '{dstExt}'"),
            };

            using var outFs = File.Create(destination);
            encoder.Encode(image, outFs);
            return (int)ExitCode.Success;
        }
        catch (DecoderException ex) { Console.Error.WriteLine($"convert: {ex.Message}"); return (int)ExitCode.FormatUnsupported; }
        catch (EncoderException ex) { Console.Error.WriteLine($"convert: {ex.Message}"); return (int)ExitCode.OutputFailed; }
        catch (IOException ex)       { Console.Error.WriteLine($"convert: {ex.Message}"); return (int)ExitCode.OutputFailed; }
    }
}

internal enum OverwritePolicy
{
    Confirm,
    Force,
    Skip,
}
