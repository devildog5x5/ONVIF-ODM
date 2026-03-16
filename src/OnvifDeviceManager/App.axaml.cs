using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OnvifDeviceManager.Platform;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dispatcher = new AvaloniaUiDispatcher();
            var clipboard = new AvaloniaClipboardService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(dispatcher, clipboard)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
