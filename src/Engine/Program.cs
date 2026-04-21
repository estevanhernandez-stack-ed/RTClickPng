using System.Runtime.InteropServices;

namespace RTClickPng.Engine;

internal static partial class Program
{
    // UCRT's getenv reads from the CRT environment block, which on Windows is not automatically
    // synced with Environment.SetEnvironmentVariable.  _putenv_s writes to the CRT block directly.
    [LibraryImport("ucrtbase", EntryPoint = "_putenv_s", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int _putenv_s(string name, string value);

    public static int Main(string[] args)
    {
        // Suppress libheif's startup plugin-scan noise (LoadLibraryA error: 193).  Unset,
        // libheif falls back to a compile-time plugin dir that contains architecture-mismatched DLLs.
        // Point it at a guaranteed-empty non-existent directory; FindFirstFile returns empty -> no
        // load attempts.  Write via both Win32 and CRT env blocks because libheif uses getenv().
        var emptyPluginDir = Path.Combine(Path.GetTempPath(), "rtclickpng-noplugins");
        Environment.SetEnvironmentVariable("LIBHEIF_PLUGIN_PATH", emptyPluginDir);
        _putenv_s("LIBHEIF_PLUGIN_PATH", emptyPluginDir);

        try
        {
            if (args.Length == 0)
            {
                PrintUsage(Console.Error);
                return (int)ExitCode.Generic;
            }

            return args[0] switch
            {
                "convert" => ConvertCommand.Run(args.AsSpan(1)),
                "copy"    => CopyCommand.Run(args.AsSpan(1)),
                "--help" or "-h" or "help" => PrintUsage(Console.Out),
                "--version" or "-v" => PrintVersion(),
                _ => UnknownVerb(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"engine: uncaught exception: {ex.GetType().Name}: {ex.Message}");
            return (int)ExitCode.UncaughtException;
        }
    }

    private static int PrintUsage(TextWriter w)
    {
        w.WriteLine("RTClickPng.Engine — out-of-process image converter (Right Click PNG)");
        w.WriteLine();
        w.WriteLine("usage:");
        w.WriteLine("  RTClickPng.Engine.exe convert <source> <destination> [--overwrite-policy=confirm|force|skip]");
        w.WriteLine("  RTClickPng.Engine.exe copy <source>");
        w.WriteLine();
        w.WriteLine("exit codes:");
        w.WriteLine("  0  success");
        w.WriteLine("  1  generic error");
        w.WriteLine("  2  source file not found");
        w.WriteLine("  3  format unsupported");
        w.WriteLine("  4  overwrite denied (policy=skip and target exists, or policy=confirm)");
        w.WriteLine("  5  output write failed");
        w.WriteLine(" 10  uncaught exception");
        return (int)ExitCode.Success;
    }

    private static int PrintVersion()
    {
        var v = typeof(Program).Assembly.GetName().Version;
        Console.Out.WriteLine($"RTClickPng.Engine {v?.Major}.{v?.Minor}.{v?.Build}");
        return (int)ExitCode.Success;
    }

    private static int UnknownVerb(string verb)
    {
        Console.Error.WriteLine($"engine: unknown verb '{verb}'. Use one of: convert, copy, --help.");
        return (int)ExitCode.Generic;
    }
}
