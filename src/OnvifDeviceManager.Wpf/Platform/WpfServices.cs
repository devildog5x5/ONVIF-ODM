using System.Windows;
using OnvifDeviceManager.ViewModels;

namespace OnvifDeviceManager.Wpf.Platform;

public class WpfUiDispatcher : IUiDispatcher
{
    public async Task InvokeAsync(Action action)
    {
        await Application.Current.Dispatcher.InvokeAsync(action);
    }

    public Task InvokeAsync(Func<Task> func)
    {
        return Application.Current.Dispatcher.InvokeAsync(func).Task;
    }
}

public class WpfClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
    {
        Clipboard.SetText(text);
        return Task.CompletedTask;
    }
}
