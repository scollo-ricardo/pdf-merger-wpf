using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class RotateTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private string? _pdfPath;
    private int _pageCount;

    // 0-based page index → cumulative rotation angle (0, 90, 180, 270)
    private readonly Dictionary<int, int> _rotations = new();

    // Currently previewed page index
    private int _previewPageIndex = -1;

    public RotateTab()
    {
        InitializeComponent();
    }

    private void BrowsePdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open PDF",
            Filter = "PDF Files|*.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        LoadPdf(dlg.FileName);
    }

    private void LoadPdf(string path)
    {
        _pdfPath = path;
        _pageCount = PdfService.GetPageCount(path);
        FileNameLabel.Text = $"{Path.GetFileName(path)}  ({_pageCount} pages)";
        _rotations.Clear();
        _previewPageIndex = -1;
        RefreshPageList();
        PreviewImage.Source = null;
        PreviewInfoText.Text = "Click a page to preview";
        PreviewRotateTransform.Angle = 0;
        StatusChanged?.Invoke(this, $"Loaded: {Path.GetFileName(path)} — {_pageCount} pages.");
    }

    private void RefreshPageList()
    {
        var selectedIndices = PageListBox.SelectedItems
            .Cast<object>()
            .Select(item => PageListBox.Items.IndexOf(item))
            .ToList();

        PageListBox.Items.Clear();

        for (int i = 0; i < _pageCount; i++)
        {
            var angle = _rotations.TryGetValue(i, out int a) ? a : 0;
            var label = angle == 0 ? $"Page {i + 1}" : $"Page {i + 1}  [{angle}°]";
            PageListBox.Items.Add(label);
        }

        // Restore selection
        foreach (var idx in selectedIndices)
        {
            if (idx >= 0 && idx < PageListBox.Items.Count)
                PageListBox.SelectedItems.Add(PageListBox.Items[idx]);
        }
    }

    private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageListBox.SelectedIndex >= 0)
        {
            _previewPageIndex = PageListBox.SelectedIndex;
            _ = RefreshPreviewAsync();
        }
    }

    private async Task RefreshPreviewAsync()
    {
        if (_pdfPath == null || _previewPageIndex < 0) return;

        PreviewInfoText.Text = "Loading…";
        var bmp = await PreviewService.RenderPageAsync(_pdfPath, _previewPageIndex, 240);
        PreviewImage.Source = bmp;

        var angle = _rotations.TryGetValue(_previewPageIndex, out int a) ? a : 0;
        PreviewRotateTransform.Angle = angle;

        var angleStr = angle == 0 ? "" : $"  [{angle}°]";
        PreviewInfoText.Text = $"Page {_previewPageIndex + 1}{angleStr}";
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        PageListBox.SelectAll();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        PageListBox.UnselectAll();
    }

    private void ApplyRotation(int delta)
    {
        if (PageListBox.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one page.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selectedIndices = PageListBox.SelectedItems
            .Cast<object>()
            .Select(item => PageListBox.Items.IndexOf(item))
            .ToList();

        foreach (var idx in selectedIndices)
        {
            var current = _rotations.TryGetValue(idx, out int a) ? a : 0;
            var newAngle = ((current + delta) % 360 + 360) % 360;
            _rotations[idx] = newAngle;
        }

        RefreshPageList();

        // Restore selection after refresh
        foreach (var idx in selectedIndices)
        {
            if (idx >= 0 && idx < PageListBox.Items.Count)
                PageListBox.SelectedItems.Add(PageListBox.Items[idx]);
        }

        if (_previewPageIndex >= 0)
            _ = RefreshPreviewAsync();
    }

    private void Rotate90Left_Click(object sender, RoutedEventArgs e) => ApplyRotation(-90);
    private void Rotate180_Click(object sender, RoutedEventArgs e) => ApplyRotation(180);
    private void Rotate90Right_Click(object sender, RoutedEventArgs e) => ApplyRotation(90);

    private void SaveRotated_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfPath == null)
        {
            MessageBox.Show("Please load a PDF first.", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_rotations.Count == 0)
        {
            MessageBox.Show("No rotations applied. Please rotate at least one page.", "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = SettingsService.Current;
        var dlg = new SaveFileDialog
        {
            Title = "Save Rotated PDF",
            Filter = "PDF Files|*.pdf",
            FileName = Path.GetFileNameWithoutExtension(_pdfPath) + "_rotated.pdf",
            InitialDirectory = string.IsNullOrWhiteSpace(settings.DefaultOutputFolder)
                ? Path.GetDirectoryName(_pdfPath)
                : settings.DefaultOutputFolder
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            StatusChanged?.Invoke(this, "Saving rotated PDF…");
            PdfService.RotatePages(_pdfPath, _rotations, dlg.FileName);
            StatusChanged?.Invoke(this, $"Saved: {Path.GetFileName(dlg.FileName)}");
            MessageBox.Show("Rotated PDF saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Save failed.");
            MessageBox.Show($"Error saving rotated PDF:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
