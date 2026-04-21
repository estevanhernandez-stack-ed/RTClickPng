using RTClickPng.Engine.Decoders;
using RTClickPng.Engine.Encoders;

namespace RTClickPng.Engine;

/// <summary>
/// Handles the <c>convert &lt;src&gt; &lt;dst&gt; [--overwrite-policy=...]</c> verb.
/// End-to-end: read source bytes → decode → encode → write to destination file.
/// ICC/EXIF/overwrite semantics arrive in item 4.
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

        // Overwrite enforcement — item 4 plumbs confirm semantics to the shell extension.
        if (File.Exists(destination) && policy == OverwritePolicy.Skip)
        {
            Console.Error.WriteLine($"convert: destination exists, policy=skip: {destination}");
            return (int)ExitCode.OverwriteDenied;
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
