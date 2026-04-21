namespace RTClickPng.Engine;

/// <summary>
/// Handles the <c>convert &lt;src&gt; &lt;dst&gt; [--overwrite-policy=...]</c> verb.
/// STUB: parses args, validates shape, emits TODO. Full decode→encode wiring lands in checklist item 3.
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
                var value = arg["--overwrite-policy=".Length..];
                policy = value switch
                {
                    "confirm" => OverwritePolicy.Confirm,
                    "force"   => OverwritePolicy.Force,
                    "skip"    => OverwritePolicy.Skip,
                    _ => OverwritePolicy.Confirm,
                };
            }
            else
            {
                Console.Error.WriteLine($"convert: unknown flag '{arg}'");
                return (int)ExitCode.Generic;
            }
        }

        // STUB — real decode/encode is item 3; ICC/EXIF/overwrite is item 4.
        _ = source;
        _ = destination;
        _ = policy;
        Console.Out.WriteLine("TODO");
        return (int)ExitCode.Success;
    }
}

internal enum OverwritePolicy
{
    Confirm,
    Force,
    Skip,
}
