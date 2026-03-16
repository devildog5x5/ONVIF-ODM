using System.Collections.ObjectModel;
using System.Windows.Input;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.ViewModels;

public class EventsViewModel : ViewModelBase
{
    private OnvifDevice? _device;
    private string _statusText = string.Empty;
    private bool _isSubscribed;
    private DeviceEvent? _selectedEvent;

    public EventsViewModel()
    {
        ClearEventsCommand = new RelayCommand(ClearEvents);
        ToggleSubscriptionCommand = new RelayCommand(ToggleSubscription);
    }

    public OnvifDevice? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public ObservableCollection<DeviceEvent> Events { get; } = new();

    public DeviceEvent? SelectedEvent
    {
        get => _selectedEvent;
        set => SetProperty(ref _selectedEvent, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsSubscribed
    {
        get => _isSubscribed;
        set => SetProperty(ref _isSubscribed, value);
    }

    public ICommand ClearEventsCommand { get; }
    public ICommand ToggleSubscriptionCommand { get; }

    public void SetDevice(OnvifDevice device)
    {
        Device = device;
        Events.Clear();
        StatusText = device.Capabilities.HasEvents
            ? "Events service available. Subscribe to start receiving events."
            : "Events service not available on this device.";
    }

    private void ClearEvents()
    {
        Events.Clear();
        StatusText = "Events cleared";
    }

    private void ToggleSubscription()
    {
        IsSubscribed = !IsSubscribed;
        StatusText = IsSubscribed
            ? "Subscribed to device events (polling)"
            : "Unsubscribed from device events";
    }

    public void AddEvent(DeviceEvent evt)
    {
        Events.Insert(0, evt);
        if (Events.Count > 1000)
        {
            Events.RemoveAt(Events.Count - 1);
        }
    }
}
