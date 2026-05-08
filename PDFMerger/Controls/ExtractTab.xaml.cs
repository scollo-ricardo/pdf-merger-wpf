using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class ExtractTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private string? _pdfPath;
    private int _pageCount;

    public ExtractTab()
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

        PageListBox.Items.Clear();
        for (int i = 1; i <= _pageCount; i++)
            PageListBox.Items.Add($"Page {i}");

        PreviewImage.Source = null;
        PreviewInfoText.Text = "Click a page to preview";
        StatusChanged?.Invoke(this, $"Loaded: {Path.GetFileName(path)} — {_pageCount} pages.");
    }

    private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageListBox.SelectedItem is string pageLabel &&
            pageLabel.StartsWith("Page ") &&
            int.TryParse(pageLabel.Substring(5), out int pageNum))
        {
            _ = ShowPreviewAsync(pageNum - 1);
        }
    }

    private async Task ShowPreviewAsync(int pageIndex)
    {
        if (_pdfPath == null) return;
        PreviewInfoText.Text = "Loading…";
        var bmp = await PreviewService.RenderPageAsync(_pdfPath, pageIndex, 240);
        PreviewImage.Source = bmp;
        PreviewInfoText.Text = $"Page {pageIndex + 1}";
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        PageListBox.SelectAll();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        PageListBox.UnselectAll();
    }

    private void DpiBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private async void Extract_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfPath == null)
        {
            MessageBox.Show("Please load a PDF first.", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var format = PngRadio.IsChecked == true ? "PNG" : "JPG";
        var ext = format.ToLower();

        if (!int.TryParse(DpiBox.Text, out int dpi) || dpi < 72 || dpi > 600)
        {
            MessageBox.Show("DPI must be between 72 and 600.", "Invalid DPI", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Determine pages to extract
        List<int> pagesToExtract;
        if (PageListBox.SelectedItems.Count > 0)
        {
            pagesToExtract = PageListBox.SelectedItems
                .Cast<string>()
                .Where(s => s.StartsWith("Page "))
                .Select(s => int.Parse(s.Substring(5)) - 1)
                .OrderBy(x => x)
                .ToList();
        }
        else
        {
            pagesToExtract = Enumerable.Range(0, _pageCount).ToList();
        }

        var folderDlg = new OpenFolderDialog
        {
            Title = "Select Output Folder"
        };
        if (folderDlg.ShowDialog() != true) return;

        var baseName = Path.GetFileNameWithoutExtension(_pdfPath);

        try
        {
            StatusChanged?.Invoke(this, "Extracting pages…");
            int done = 0;

            foreach (var pageIndex in pagesToExtract)
            {
                var outputPath = Path.Combine(folderDlg.FolderName, $"{baseName}_page{pageIndex + 1}.{ext}");
                await PreviewService.ExtractPageAsImageAsync(_pdfPath, pageIndex, outputPath, format, dpi);
                done++;
                StatusChanged?.Invoke(this, $"Extracting… {done}/{pagesToExtract.Count}");
            }

            StatusChanged?.Invoke(this, $"Extracted {done} page(s) to {folderDlg.FolderName}");
            MessageBox.Show($"Extracted {done} page(s) successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Extraction failed.");
            MessageBox.Show($"Error extracting pages:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
