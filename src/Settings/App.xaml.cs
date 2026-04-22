using Microsoft.UI.Xaml;

namespace RTClickPng.Settings;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        // FIRST statement of the ctor — any earlier failure is in the WinUI bootstrap before us.
        Log("ctor/start");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log($"AppDomain: {(e.ExceptionObject as Exception)?.Message}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
            Log($"TaskScheduler: {e.Exception.Message}");

        try
        {
            InitializeComponent();
            Log("ctor/initialize-component-done");
            UnhandledException += (_, e) => Log($"XAML: {e.Exception.Message}");
            Log("ctor/complete");
        }
        catch (Exception ex)
        {
            Log($"ctor/exception: {ex.GetType().FullName}: {ex.Message}");
            Log(ex.StackTrace ?? "");
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log("OnLaunched/start");
        try
        {
            _window = new MainWindow();
            _window.Activate();
            Log("OnLaunched/complete");
        }
        catch (Exception ex)
        {
            Log($"OnLaunched/exception: {ex.GetType().FullName}: {ex.Message}");
            Log(ex.StackTrace ?? "");
            throw;
        }
    }

    internal static void Log(string message)
    {
        try
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(dir)) dir = Path.GetTempPath();
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "RTClickPng.Settings.crash.log"),
                $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
        catch { /* best-effort */ }
    }
}
