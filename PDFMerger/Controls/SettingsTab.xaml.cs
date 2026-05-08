using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class SettingsTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private bool _loading = false;

    public SettingsTab()
    {
        InitializeComponent();
        Loaded += SettingsTab_Loaded;
    }

    private void SettingsTab_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        _loading = true;

        var s = SettingsService.Current;

        LightThemeRadio.IsChecked = s.Theme != "Dark";
        DarkThemeRadio.IsChecked = s.Theme == "Dark";
        OutputFolderBox.Text = s.DefaultOutputFolder;
        MergeFilenameBox.Text = s.DefaultMergeFilename;
        AutoOpenCheckBox.IsChecked = s.AutoOpenFolder;

        _loading = false;
    }

    private void SaveSettings()
    {
        if (!IsLoaded || _loading) return;

        var s = SettingsService.Current;
        s.Theme = DarkThemeRadio.IsChecked == true ? "Dark" : "Light";
        s.DefaultOutputFolder = OutputFolderBox.Text;
        s.DefaultMergeFilename = MergeFilenameBox.Text;
        s.AutoOpenFolder = AutoOpenCheckBox.IsChecked == true;

        SettingsService.Save(s);
        StatusChanged?.Invoke(this, "Settings saved.");
    }

    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _loading) return;

        var newTheme = DarkThemeRadio.IsChecked == true ? "Dark" : "Light";
        var current = SettingsService.Current;
        if (current.Theme != newTheme)
        {
            current.Theme = newTheme;
            SettingsService.Save(current);

            ModernWpf.ThemeManager.Current.ApplicationTheme =
                newTheme == "Dark"
                    ? ModernWpf.ApplicationTheme.Dark
                    : ModernWpf.ApplicationTheme.Light;

            StatusChanged?.Invoke(this, $"Theme changed to {newTheme}.");
        }
    }

    private void OutputFolderBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SaveSettings();
    }

    private void MergeFilenameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SaveSettings();
    }

    private void AutoOpenCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Default Output Folder"
        };

        if (dlg.ShowDialog() == true)
        {
            OutputFolderBox.Text = dlg.FolderName;
            SaveSettings();
        }
    }
}
