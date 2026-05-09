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

        // First-run install prompt: only when running outside the install dir
        // and the user hasn't been asked before.
        if (InstallerService.ShouldShowInstallPrompt())
        {
            var dialog = new InstallDialog();
            var result = dialog.ShowDialog();

            if (result != true)
            {
                // User closed the dialog with the [X]; don't mark as shown so
                // they get another chance next launch.
                Shutdown();
                return;
            }

            // Either Install or Run-without-installing: don't ask again.
            settings.InstallPromptShown = true;
            SettingsService.Save(settings);

            if (dialog.ShouldInstall)
            {
                try
                {
                    InstallerService.Install(
                        dialog.InstallPath,
                        dialog.CreateDesktopShortcut,
                        dialog.CreateStartMenuShortcut);
                    Shutdown();
                    return;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(
                        $"Couldn't install:\n{ex.Message}\n\nYou can keep using the app from its current location.",
                        "Install Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Fall through to launch the main window from the current location.
                }
            }
        }

        new MainWindow().Show();
    }
}
