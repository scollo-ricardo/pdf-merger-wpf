using System.Diagnostics;
using System.Windows;
using PDFMerger.Services;

namespace PDFMerger;

public partial class MainWindow : Window
{
    private string? _updateUrl;

    public MainWindow()
    {
        InitializeComponent();
        WireEvents();
        Loaded += MainWindow_Loaded;
    }

    private void WireEvents()
    {
        MergeTabControl.StatusChanged += (s, msg) => SetStatus(msg);
        SplitTabControl.StatusChanged += (s, msg) => SetStatus(msg);
        ExtractTabControl.StatusChanged += (s, msg) => SetStatus(msg);
        RotateTabControl.StatusChanged += (s, msg) => SetStatus(msg);
        CompressTabControl.StatusChanged += (s, msg) => SetStatus(msg);
        SettingsTabControl.StatusChanged += (s, msg) => SetStatus(msg);
    }

    public void SetStatus(string msg)
    {
        Dispatcher.InvokeAsync(() => StatusText.Text = msg);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var info = await UpdateService.CheckForUpdateAsync();
        if (info == null) return;

        _updateUrl = info.ReleasePageUrl;
        UpdateBannerText.Text = $"Version {info.LatestVersion} is available — click to download";
        UpdateBanner.Visibility = Visibility.Visible;
    }

    private void UpdateBanner_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (string.IsNullOrEmpty(_updateUrl)) return;
        Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
    }

    private void DismissBanner_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
        // Stop bubbling so we don't also trigger UpdateBanner_Click.
        e.Handled = true;
    }
}
