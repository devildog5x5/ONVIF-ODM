using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Platform;

public class AvaloniaUiDispatcher : IUiDispatcher
{
    public async Task InvokeAsync(Action action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
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
