using Microsoft.UI.Xaml;

namespace RTClickPng.Settings;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        App.Log("MainWindow/ctor start");
        InitializeComponent();
        App.Log("MainWindow/ctor InitializeComponent done");
    }
}
