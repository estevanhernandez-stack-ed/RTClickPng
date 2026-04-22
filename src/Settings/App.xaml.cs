using System;
using System.IO;
using System.Windows;

namespace RTClickPng.Settings;

/// <summary>
/// Minimal WPF shell — MainWindow.xaml is the StartupUri.  Logs first-line of each lifecycle
/// event so we can diagnose silent exits on packaged machines.
/// </summary>
public partial class App : Application
{
    public App()
    {
        Log("App/ctor start");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log($"AppDomain: {(e.ExceptionObject as Exception)?.Message}");
        DispatcherUnhandledException += (_, e) =>
            Log($"Dispatcher: {e.Exception.Message}");
        Log("App/ctor done");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("App/OnStartup start");
        base.OnStartup(e);
        Log("App/OnStartup done");
    }

    internal static void Log(string msg)
    {
        try
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(dir)) dir = Path.GetTempPath();
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "RTClickPng.Settings.crash.log"),
                $"[{DateTime.Now:O}] {msg}{Environment.NewLine}");
        }
        catch { /* best-effort */ }
    }
}
