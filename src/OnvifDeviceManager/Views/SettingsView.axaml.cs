using Avalonia.Controls;
using Avalonia.Interactivity;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OpenAssetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string url || string.IsNullOrWhiteSpace(url))
            return;
        if (SettingsRoot.DataContext is SettingsViewModel vm)
            vm.OpenDownloadUrl(url);
    }
}
