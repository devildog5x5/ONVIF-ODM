using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Platform;

public class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <summary>
    /// Must re-enter safely when already on the UI thread; otherwise <see cref="LiveViewViewModel.RefreshSnapshotAsync"/>
    /// (and similar) can deadlock when started from <see cref="AsyncRelayCommand"/> on the UI thread.
    /// </summary>
    public async Task InvokeAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Normal);
    }

    public async Task InvokeAsync(Func<Task> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
            await func().ConfigureAwait(true);
        else
            await Dispatcher.UIThread.InvokeAsync(func, DispatcherPriority.Normal);
    }
}

public class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        var topLevel = Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;

        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(text);
    }
}
