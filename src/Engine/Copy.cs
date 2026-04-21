namespace RTClickPng.Engine;

/// <summary>
/// Handles the <c>copy &lt;src&gt;</c> verb — produces PNG bytes on stdout with a 4-byte length prefix
/// so the Shell Extension can safely bound the read. STUB: parses arg, emits TODO.
/// Full encode pipeline lands in checklist item 3.
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

        // STUB — real decode + png-encode-to-stdout wiring is item 3.
        _ = source;
        Console.Out.WriteLine("TODO");
        return (int)ExitCode.Success;
    }
}
