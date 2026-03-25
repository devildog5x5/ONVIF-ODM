using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OnvifDeviceManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => ApplyWindowIcon();
    }

    private void ApplyWindowIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "warrior_icon.ico");
            if (File.Exists(path))
            {
                using var fs = File.OpenRead(path);
                Icon = new WindowIcon(fs);
            }
            else
            {
                using var stream = AssetLoader.Open(new Uri("avares://OnvifDeviceManager/Assets/warrior_icon.ico"));
                Icon = new WindowIcon(stream);
            }
        }
        catch
        {
            /* XAML Icon= remains */
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "warrior_icon.ico");
            if (File.Exists(path))
            {
                TitleBrandImage.Source = new Bitmap(path);
                return;
            }
        }
        catch { /* use XAML */ }

        try
        {
            using var s = AssetLoader.Open(new Uri("avares://OnvifDeviceManager/Assets/branding_master.png"));
            TitleBrandImage.Source = new Bitmap(s);
        }
        catch { /* keep XAML warrior ico */ }
    }
}
