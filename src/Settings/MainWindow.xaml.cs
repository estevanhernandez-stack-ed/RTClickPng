using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using RTClickPng.Settings.Services;
using RTClickPng.Shared;
using Windows.Graphics;

namespace RTClickPng.Settings;

public sealed partial class MainWindow : Window
{
    private readonly SettingsService _service = new();
    private SettingsSchema _current;
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();
        _current = _service.Read();

        // Fixed-size, non-resizable Fluent window per spec.  Content-sized by XAML Grid padding.
        if (AppWindow is { } w)
        {
            w.Resize(new SizeInt32(520, 340));
            if (w.Presenter is OverlappedPresenter op)
            {
                op.IsResizable = false;
                op.IsMaximizable = false;
            }
        }

        ShowJpegVariantsToggle.IsOn = _current.ShowJpegVariants;
        ConfirmOverwriteToggle.IsOn = _current.ConfirmBeforeOverwrite;
        _loaded = true;
        StatusText.Text = $"Settings file: {Paths.SettingsJsonPath}";
    }

    private void OnShowJpegVariantsToggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _current.ShowJpegVariants = ((ToggleSwitch)sender).IsOn;
        _service.Write(_current);
    }

    private void OnConfirmOverwriteToggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _current.ConfirmBeforeOverwrite = ((ToggleSwitch)sender).IsOn;
        _service.Write(_current);
    }
}
