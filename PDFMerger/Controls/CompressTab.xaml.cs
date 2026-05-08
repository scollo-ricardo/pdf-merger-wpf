using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class CompressTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private string? _pdfPath;

    public CompressTab()
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

        _pdfPath = dlg.FileName;
        var sizeBytes = new FileInfo(_pdfPath).Length;
        var sizeMb = sizeBytes / 1024.0 / 1024.0;
        FileNameLabel.Text = $"{Path.GetFileName(_pdfPath)}  ({sizeMb:F2} MB)";

        SizeResultBorder.Visibility = Visibility.Collapsed;
        StatusChanged?.Invoke(this, $"Loaded: {Path.GetFileName(_pdfPath)}  ({sizeMb:F2} MB)");
    }

    private void CompressSave_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfPath == null)
        {
            MessageBox.Show("Please load a PDF first.", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string level = "Low";
        if (MediumRadio.IsChecked == true) level = "Medium";
        else if (HighRadio.IsChecked == true) level = "High";

        var settings = SettingsService.Current;
        var dlg = new SaveFileDialog
        {
            Title = "Save Compressed PDF",
            Filter = "PDF Files|*.pdf",
            FileName = Path.GetFileNameWithoutExtension(_pdfPath) + "_compressed.pdf",
            InitialDirectory = string.IsNullOrWhiteSpace(settings.DefaultOutputFolder)
                ? Path.GetDirectoryName(_pdfPath)
                : settings.DefaultOutputFolder
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            StatusChanged?.Invoke(this, "Compressing PDF…");

            var originalSize = new FileInfo(_pdfPath).Length;
            PdfService.CompressPdf(_pdfPath, dlg.FileName, level);
            var newSize = new FileInfo(dlg.FileName).Length;

            var origMb = originalSize / 1024.0 / 1024.0;
            var newMb = newSize / 1024.0 / 1024.0;
            double reduction = originalSize > 0
                ? (1.0 - (double)newSize / originalSize) * 100.0
                : 0;

            StatusChanged?.Invoke(this, $"Compressed: {origMb:F2} MB → {newMb:F2} MB ({reduction:F1}% reduction)");

            SizeResultBorder.Visibility = Visibility.Visible;

            if (reduction >= 0)
            {
                SizeLabel.Text = $"Before: {origMb:F2} MB    →    After: {newMb:F2} MB    ({reduction:F1}% reduction)";
            }
            else
            {
                var increase = -reduction;
                SizeLabel.Text = $"Before: {origMb:F2} MB    →    After: {newMb:F2} MB    ({increase:F1}% larger — file was already well-compressed)";
            }

            MessageBox.Show(
                $"PDF compressed successfully!\n\nOriginal: {origMb:F2} MB\nCompressed: {newMb:F2} MB\nReduction: {reduction:F1}%\n\nSaved to:\n{dlg.FileName}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Compression failed.");
            MessageBox.Show($"Error compressing PDF:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
