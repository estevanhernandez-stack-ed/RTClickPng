using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RTClickPng.Engine;

internal static partial class Program
{
    // Module initializer runs once when the assembly is loaded — both for RTClickPng.Engine.exe
    // (via Main) and for Engine.Tests.dll (via xUnit's first touch of an internal type).  Without
    // this, tests fail with DllNotFoundException because their Test Host's working directory is
    // not our output dir.
    [ModuleInitializer]
    internal static void BootstrapNativeLoaders()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        if (Directory.Exists(baseDir))
        {
            SetDllDirectoryW(baseDir);
            PreloadNativeDlls(baseDir);
        }
    }

    // UCRT's getenv reads from the CRT environment block, which on Windows is not automatically
    // synced with Environment.SetEnvironmentVariable.  _putenv_s writes to the CRT block directly.
    [LibraryImport("ucrtbase", EntryPoint = "_putenv_s", StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int _putenv_s(string name, string value);

    [LibraryImport("kernel32", EntryPoint = "SetDllDirectoryW", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetDllDirectoryW(string path);

    /// <summary>
    /// Load every *.dll in <paramref name="dir"/> into the process.  Once each native DLL is
    /// already mapped, the loader resolves transitive dependencies from the process module list
    /// rather than re-searching file system paths — so libwebp.dll's reference to libsharpyuv.dll
    /// finds the already-loaded instance regardless of current directory / PATH state.
    /// </summary>
    private static void PreloadNativeDlls(string dir)
    {
        // Known list of DLLs we bundle (+ their transitive deps from vcpkg).  Explicit list
        // avoids loading stray PDBs or unrelated binaries that might end up next to the exe.
        string[] dlls =
        [
            "libsharpyuv.dll",   // libwebp dep — load first
            "dav1d.dll",         // libavif dep
            "libde265.dll",      // libheif dep
            "libyuv.dll",        // libavif support
            "zlib1.dll",         // libspng / libheif dep
            "libwebp.dll", "libwebpdecoder.dll", "libwebpdemux.dll", "libwebpmux.dll",
            "avif.dll",
            "heif.dll",
            "spng.dll",
            "jpeg62.dll", "turbojpeg.dll",
        ];
        foreach (var name in dlls)
        {
            var path = Path.Combine(dir, name);
            if (!File.Exists(path)) continue;
            try { System.Runtime.InteropServices.NativeLibrary.Load(path); }
            catch { /* best-effort; individual failures surface at first P/Invoke */ }
        }
    }

    public static int Main(string[] args)
    {
        // Prepend the exe's own directory to DLL search path.  .NET 9 AOT does not honor
        // DllImportSearchPath.AssemblyDirectory consistently for [LibraryImport]-generated
        // stubs when launched from some working directories; doing it explicitly via
        // SetDllDirectoryW lets native DLLs next to the exe resolve reliably.
        // Preload every native DLL next to the exe so later [LibraryImport] calls resolve.
        // .NET 9 AOT + LoadLibrary's default resolution policy doesn't search the DLL's own
        // directory for transitive deps (libwebp.dll -> libsharpyuv.dll, etc.), so we front-load.
        var baseDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        SetDllDirectoryW(baseDir);   // for the [LibraryImport]-generated LoadLibrary
        PreloadNativeDlls(baseDir);

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
