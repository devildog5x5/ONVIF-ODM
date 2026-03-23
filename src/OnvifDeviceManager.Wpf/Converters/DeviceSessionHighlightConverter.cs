using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Wpf.Converters;

/// <summary>Highlights the discovery list row that matches the active app session (same camera address).</summary>
public class DeviceSessionHighlightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not OnvifDevice device
            || values[1] is not string activeAddr || string.IsNullOrEmpty(activeAddr))
            return Brushes.Transparent;

        if (device.Address != activeAddr)
            return Brushes.Transparent;

        return new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0xD4, 0xAA));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
