using System;
using System.IO;
using System.Windows;
using RTClickPng.Shared;

namespace RTClickPng.Settings;

/// <summary>
/// Minimal WPF shell — MainWindow.xaml is the StartupUri.  Writes a failure log only when
/// something throws, into the package's LocalState folder (see <see cref="Paths.CrashLogPath"/>).
/// </summary>
public partial class App : Application
{
    public App()
    {
        DeleteLegacyCrashLog();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogFailure("AppDomain", e.ExceptionObject as Exception);
        DispatcherUnhandledException += (_, e) =>
            LogFailure("Dispatcher", e.Exception);
    }

    /// <summary>
    /// Append one line: where it failed, the exception type, and its message.  Never file contents.
    /// </summary>
    internal static void LogFailure(string where, Exception? ex)
    {
        try
        {
            var path = Paths.CrashLogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(
                path,
                $"[{DateTime.Now:O}] {where}: {ex?.GetType().Name}: {ex?.Message}{Environment.NewLine}");
        }
        catch { /* best-effort */ }
    }

    /// <summary>
    /// 0.1.1.0 and earlier wrote a per-launch trace to the LocalAppData root, outside the package
    /// folder, where uninstall can't reach it.  Remove it so updated installs come clean.
    /// </summary>
    private static void DeleteLegacyCrashLog()
    {
        try { File.Delete(Paths.LegacyCrashLogPath); }
        catch { /* best-effort */ }
    }
}
