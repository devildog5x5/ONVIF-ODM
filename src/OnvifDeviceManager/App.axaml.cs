using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OnvifDeviceManager.Platform;
using OnvifDeviceManager.Services;
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
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            CrashLogger.Log("AppDomain.UnhandledException", ex ?? new Exception("Unknown fatal error"));
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            CrashLogger.Log("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

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
