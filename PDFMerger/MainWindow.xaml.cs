using System.Windows;

namespace PDFMerger;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WireEvents();
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
}
