using System.Windows;
using System.Windows.Threading;
using OnvifDeviceManager.Services;

namespace OnvifDeviceManager.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            CrashLogger.Log("AppDomain.UnhandledException", ex ?? new Exception("Unknown fatal error"));
            var detail = ex != null ? CrashLogger.FormatExceptionForUser(ex) : "Unknown fatal error";
            MessageBox.Show(
                $"A fatal error occurred:\n\n{detail}\n\nA log has been saved to:\n{CrashLogger.LogFilePath}",
                "ONVIF Device Manager - Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            CrashLogger.Log("Dispatcher.UnhandledException", args.Exception);
            MessageBox.Show(
                $"An error occurred:\n\n{CrashLogger.FormatExceptionForUser(args.Exception)}\n\nThe application will continue.\nLog: {CrashLogger.LogFilePath}",
                "ONVIF Device Manager - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            CrashLogger.Log("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };
    }
}
