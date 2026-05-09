using System.Windows;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger;

public partial class InstallDialog : Window
{
    public bool ShouldInstall { get; private set; }
    public string InstallPath => LocationBox.Text;
    public bool CreateDesktopShortcut => DesktopCheck.IsChecked == true;
    public bool CreateStartMenuShortcut => StartMenuCheck.IsChecked == true;

    public InstallDialog()
    {
        InitializeComponent();
        LocationBox.Text = InstallerService.DefaultInstallDir;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Install Folder",
            InitialDirectory = LocationBox.Text
        };
        if (dlg.ShowDialog() == true)
            LocationBox.Text = System.IO.Path.Combine(dlg.FolderName, "PDFMerger");
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LocationBox.Text))
        {
            MessageBox.Show(this, "Please choose an install location.", "Missing path",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ShouldInstall = true;
        DialogResult = true;
        Close();
    }

    private void RunOnce_Click(object sender, RoutedEventArgs e)
    {
        ShouldInstall = false;
        DialogResult = true;
        Close();
    }
}
