using System.Windows;
using RTClickPng.Settings.Services;
using RTClickPng.Shared;

namespace RTClickPng.Settings;

public partial class MainWindow : Window
{
    private readonly SettingsService _service = new();
    private SettingsSchema _current = new();
    private bool _loaded;

    public MainWindow()
    {
        App.Log("MainWindow/ctor start");
        InitializeComponent();
        App.Log("MainWindow/ctor post-InitializeComponent");
        try
        {
            _current = _service.Read();
            App.Log("MainWindow/ctor settings read");
            ShowJpegToggle.IsChecked = _current.ShowJpegVariants;
            ConfirmOverwriteToggle.IsChecked = _current.ConfirmBeforeOverwrite;
            StatusText.Text = Paths.SettingsJsonPath;
            App.Log("MainWindow/ctor status set");
            _loaded = true;
        }
        catch (System.Exception ex)
        {
            App.Log($"MainWindow/ctor failed: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
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
