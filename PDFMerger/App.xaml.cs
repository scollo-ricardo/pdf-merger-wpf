using System.Windows;
using PDFMerger.Services;

namespace PDFMerger;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsService.Load();
        var settings = SettingsService.Current;

        ModernWpf.ThemeManager.Current.ApplicationTheme =
            settings.Theme == "Dark"
                ? ModernWpf.ApplicationTheme.Dark
                : ModernWpf.ApplicationTheme.Light;
    }
}
