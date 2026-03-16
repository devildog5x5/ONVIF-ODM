using System.Windows;

namespace OnvifDeviceManager.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "ONVIF Device Manager - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
