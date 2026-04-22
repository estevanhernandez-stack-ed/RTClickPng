using System.Windows;
using RTClickPng.Settings.Services;
using RTClickPng.Shared;

namespace RTClickPng.Settings;

public partial class MainWindow : Window
{
    private readonly SettingsService _service = new();
    private SettingsSchema _current;
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();
        _current = _service.Read();
        ShowJpegToggle.IsChecked = _current.ShowJpegVariants;
        ConfirmOverwriteToggle.IsChecked = _current.ConfirmBeforeOverwrite;
        StatusText.Text = Paths.SettingsJsonPath;
        _loaded = true;
    }

    private void OnShowJpegToggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _current.ShowJpegVariants = ShowJpegToggle.IsChecked == true;
        _service.Write(_current);
    }

    private void OnConfirmOverwriteToggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _current.ConfirmBeforeOverwrite = ConfirmOverwriteToggle.IsChecked == true;
        _service.Write(_current);
    }
}
