using System.Windows;
using OnvifDeviceManager.ViewModels;
using OnvifDeviceManager.Wpf.Platform;

namespace OnvifDeviceManager.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new WpfUiDispatcher(), new WpfClipboardService());
    }
}
