namespace OnvifDeviceManager.ViewModels;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);

    Task InvokeAsync(Func<Task> func);
}

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
