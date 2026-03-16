namespace OnvifDeviceManager.ViewModels;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);
}

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
