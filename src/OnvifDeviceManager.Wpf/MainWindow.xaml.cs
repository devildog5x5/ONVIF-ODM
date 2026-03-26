using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OnvifDeviceManager.ViewModels;
using OnvifDeviceManager.Wpf.Platform;

namespace OnvifDeviceManager.Wpf;

public partial class MainWindow : Window
{
    private const string PackAsm = "pack://application:,,,/OnvifDeviceManager.Wpf;component/";

    public MainWindow()
    {
        InitializeComponent();
        ApplyBrandingImages();
        DataContext = new MainViewModel(new WpfUiDispatcher(), new WpfClipboardService());
        // Re-apply after HWND exists so caption + taskbar reliably pick up the icon (published exe + sidecar .ico).
        SourceInitialized += (_, _) => ApplyWindowIconFromDiskOrPack();
    }

    private void ApplyWindowIconFromDiskOrPack()
    {
        try
        {
            var icon = LoadWarriorImageSource();
            if (icon != null)
                Icon = icon;
        }
        catch
        {
            /* keep existing */
        }
    }

    private static ImageSource? LoadWarriorImageSource()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "warrior_icon.ico");
            if (File.Exists(path))
                return BitmapFrame.Create(new Uri(Path.GetFullPath(path), UriKind.Absolute));
        }
        catch { /* fall through */ }

        try
        {
            return BitmapFrame.Create(new Uri(PackAsm + "warrior_icon.ico", UriKind.Absolute));
        }
        catch
        {
            return null;
        }
    }

    private void ApplyBrandingImages()
    {
        try
        {
            var icon = LoadWarriorImageSource();
            if (icon != null)
                Icon = icon;
        }
        catch
        {
            /* XAML Icon may still apply */
        }

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            var brandPath = Path.Combine(AppContext.BaseDirectory, "branding_master.png");
            bmp.UriSource = File.Exists(brandPath)
                ? new Uri(Path.GetFullPath(brandPath), UriKind.Absolute)
                : new Uri(PackAsm + "branding_master.png", UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            TitleBrandImage.Source = bmp;
        }
        catch
        {
            try
            {
                TitleBrandImage.Source = LoadWarriorImageSource();
            }
            catch { /* ignore */ }
        }
    }
}
