using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        // Dark caption glyphs (min/max/close) + dark system accents.  Available on Win10 19041+.
        int useDark = 1;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

        // Caption, caption-text, and border colors.  Win11 22000+ attributes (we target 22621).
        int caption = Bgr(0x0F, 0x1F, 0x31);   // #0F1F31 — matches window body
        _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref caption, sizeof(int));

        int text = Bgr(0xE6, 0xEB, 0xF3);      // #E6EBF3 — matches body text
        _ = DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
    }

    // DWM uses COLORREF (0x00BBGGRR) — pack R,G,B bytes into the low 24 bits in BGR order.
    private static int Bgr(byte r, byte g, byte b) => (b << 16) | (g << 8) | r;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

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
