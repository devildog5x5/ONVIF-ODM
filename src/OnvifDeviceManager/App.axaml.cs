using System.Net.Sockets;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        // Avalonia UI-thread exceptions do not reliably surface through AppDomain.UnhandledException; hook explicitly.
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            // LibVLC VideoView (NativeControlHost) can throw repeatedly during layout/scroll when Win32
            // child window creation fails. Logging + a dialog per failure makes the app unusable.
            if (IsWin32NativeControlHostCreateFailure(e.Exception))
            {
                LogThrottledNativeHostFailure("Avalonia.Dispatcher.UnhandledException (NativeControlHost)", e.Exception);
                e.Handled = true;
                return;
            }

            CrashLogger.Log("Avalonia.Dispatcher.UnhandledException", e.Exception);
            ShowErrorMessage(
                "ONVIF Device Manager — Error",
                $"An error occurred:\n\n{CrashLogger.FormatExceptionForUser(e.Exception)}\n\nThe application will try to continue.\nLog: {CrashLogger.LogFilePath}",
                warning: true);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            CrashLogger.Log("AppDomain.UnhandledException", ex ?? new Exception("Unknown fatal error"));
            if (ex != null)
            {
                ShowErrorMessage(
                    "ONVIF Device Manager — Fatal error",
                    $"A fatal error occurred:\n\n{CrashLogger.FormatExceptionForUser(ex)}\n\nLog: {CrashLogger.LogFilePath}",
                    warning: false);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            if (IsBenignUnobservedTaskException(args.Exception))
            {
                args.SetObserved();
                return;
            }

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

    private static DateTime _lastNativeHostDispatcherLogUtc;

    private static void LogThrottledNativeHostFailure(string context, Exception ex)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastNativeHostDispatcherLogUtc).TotalSeconds < 45)
            return;
        _lastNativeHostDispatcherLogUtc = now;
        CrashLogger.Log(context, ex);
    }

    /// <summary>
    /// Avalonia throws this when CreateWindowEx fails for the LibVLC child HWND (layout/scroll can retry often).
    /// </summary>
    private static bool IsWin32NativeControlHostCreateFailure(Exception? ex)
    {
        if (ex == null) return false;
        var stack = new Stack<Exception>();
        stack.Push(ex);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is InvalidOperationException iox
                && iox.Message.Contains("native control host", StringComparison.OrdinalIgnoreCase))
                return true;
            if (current.InnerException != null)
                stack.Push(current.InnerException);
            if (current is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                    stack.Push(inner);
            }
        }

        return false;
    }

    private static bool IsBenignUnobservedTaskException(AggregateException agg)
    {
        foreach (var ex in agg.Flatten().InnerExceptions)
        {
            if (ex is OperationCanceledException)
                return true;
            if (ex is ObjectDisposedException)
                return true;
            if (ex is IOException io && io.InnerException is SocketException)
                return true;
            if (ex is SocketException)
                return true;
            if (ex.Message.Contains("I/O operation has been aborted", StringComparison.OrdinalIgnoreCase))
                return true;
            if (IsWin32NativeControlHostCreateFailure(ex))
                return true;
        }

        return false;
    }

    private static void ShowErrorMessage(string title, string text, bool warning)
    {
        if (OperatingSystem.IsWindows())
        {
            const uint mb_ok = 0;
            const uint mb_iconwarning = 0x30;
            const uint mb_iconerror = 0x10;
            UiMessageBox.MessageBoxW(0, text, title, mb_ok | (warning ? mb_iconwarning : mb_iconerror));
            return;
        }

        Console.Error.WriteLine(title);
        Console.Error.WriteLine(text);
    }

    private static class UiMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
        internal static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);
    }
}
