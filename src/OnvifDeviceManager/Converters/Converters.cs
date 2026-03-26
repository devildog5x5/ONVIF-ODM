using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Converters;

public class StatusToColorConverter : IValueConverter
{
    public static readonly StatusToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DeviceStatus.Online => SolidColorBrush.Parse("#00D4AA"),
            DeviceStatus.Offline => SolidColorBrush.Parse("#E94560"),
            DeviceStatus.Authenticating => SolidColorBrush.Parse("#FFC107"),
            DeviceStatus.Error => SolidColorBrush.Parse("#E94560"),
            _ => SolidColorBrush.Parse("#607D8B")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntToBoolConverter : IValueConverter
{
    public static readonly IntToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BytesToBitmapConverter : IValueConverter
{
    public static readonly BytesToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch
        {
            // Cameras may return HTML/XML instead of JPEG; avoid tearing down the UI thread.
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
