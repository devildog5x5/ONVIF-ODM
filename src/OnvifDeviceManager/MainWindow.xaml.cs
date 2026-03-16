using System.Windows;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
