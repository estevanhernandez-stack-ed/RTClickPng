using Microsoft.UI.Xaml;

namespace RTClickPng.Settings;

/// <summary>
/// Single-window WinUI 3 app.  The only thing this does is stand up <see cref="MainWindow"/>
/// on launch and keep the process alive until the user closes it.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
