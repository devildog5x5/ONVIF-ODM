using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Converters;

public class DeviceSessionHighlightConverter : IMultiValueConverter
{
    public static readonly DeviceSessionHighlightConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not OnvifDevice device
            || values[1] is not string activeAddr || string.IsNullOrEmpty(activeAddr))
            return Brushes.Transparent;

        return device.Address == activeAddr
            ? SolidColorBrush.Parse("#3300D4AA")
            : Brushes.Transparent;
    }
}
