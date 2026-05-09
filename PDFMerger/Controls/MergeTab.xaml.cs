using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PDFMerger.Models;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class MergeTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private readonly ObservableCollection<FileItem> _files = new();
    private int _currentPreviewPage = 0;
    private int _previewPageCount = 0;
    private string? _previewFilePath = null;

    // Drag-to-reorder state
    private Point _dragStartPoint;
    private bool _isDraggingInternal = false;

    public MergeTab()
    {
        InitializeComponent();
        FileListBox.ItemsSource = _files;
        FileListBox.KeyDown += FileListBox_KeyDown;
    }

    // ─── File adding ──────────────────────────────────────────────

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Add Files",
            Filter = "Supported Files|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.tiff|PDF Files|*.pdf|Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff|All Files|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() == true)
        {
            foreach (var path in dlg.FileNames)
                _files.Add(new FileItem(path));

            UpdateDropHint();
            StatusChanged?.Invoke(this, $"{_files.Count} file(s) in queue.");
        }
    }

    // ─── DragDrop from Explorer ───────────────────────────────────

    private void FileListBox_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void FileListBox_DragOver(object sender, DragEventArgs e)
    {
        if (_isDraggingInternal)
        {
            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void FileListBox_Drop(object sender, DragEventArgs e)
    {
        if (_isDraggingInternal)
        {
            // Internal reorder drop
            HandleInternalDrop(e);
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validExts = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };

            foreach (var path in files)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (validExts.Contains(ext))
                    _files.Add(new FileItem(path));
            }
            UpdateDropHint();
            StatusChanged?.Invoke(this, $"{_files.Count} file(s) in queue.");
        }
        e.Handled = true;
    }

    // ─── Internal drag-to-reorder ─────────────────────────────────

    private void FileListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void FileListBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (_files.Count == 0) return;
        if (FileListBox.SelectedItem == null) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDraggingInternal = true;
        var selectedIndex = FileListBox.SelectedIndex;
        DragDrop.DoDragDrop(FileListBox, selectedIndex.ToString(), DragDropEffects.Move);
        _isDraggingInternal = false;
    }

    private void HandleInternalDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;

        var indexStr = (string)e.Data.GetData(DataFormats.StringFormat);
        if (!int.TryParse(indexStr, out int sourceIndex)) return;

        // Find the target item under the drop position
        var listBox = FileListBox;
        var pos = e.GetPosition(listBox);
        int targetIndex = -1;

        for (int i = 0; i < listBox.Items.Count; i++)
        {
            var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (item == null) continue;
            var itemPos = item.TranslatePoint(new Point(0, 0), listBox);
            if (pos.Y < itemPos.Y + item.ActualHeight)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0) targetIndex = _files.Count - 1;
        if (sourceIndex == targetIndex) return;
        if (sourceIndex < 0 || sourceIndex >= _files.Count) return;
        if (targetIndex < 0 || targetIndex >= _files.Count) return;

        var item2 = _files[sourceIndex];
        _files.RemoveAt(sourceIndex);
        _files.Insert(targetIndex, item2);
        FileListBox.SelectedIndex = targetIndex;
    }

    // ─── Toolbar buttons ──────────────────────────────────────────

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        int idx = FileListBox.SelectedIndex;
        if (idx > 0)
        {
            _files.Move(idx, idx - 1);
            FileListBox.SelectedIndex = idx - 1;
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        int idx = FileListBox.SelectedIndex;
        if (idx >= 0 && idx < _files.Count - 1)
        {
            _files.Move(idx, idx + 1);
            FileListBox.SelectedIndex = idx + 1;
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelected();
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        ClearPreview();
        StatusChanged?.Invoke(this, "File list cleared.");
    }

    private void FileListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
            RemoveSelected();
    }

    private void UpdateDropHint() =>
        DropHint.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void RemoveSelected()
    {
        var selected = FileListBox.SelectedItems.Cast<FileItem>().ToList();
        foreach (var item in selected)
            _files.Remove(item);

        if (_files.Count == 0) ClearPreview();
        UpdateDropHint();
        StatusChanged?.Invoke(this, $"{_files.Count} file(s) in queue.");
    }

    // ─── Preview ──────────────────────────────────────────────────

    private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListBox.SelectedItem is FileItem item)
            _ = LoadPreviewAsync(item);
        else
            ClearPreview();
    }

    private async Task LoadPreviewAsync(FileItem item)
    {
        _previewFilePath = item.Path;
        _currentPreviewPage = 0;

        if (item.IsPdf)
        {
            _previewPageCount = PdfService.GetPageCount(item.Path);
            await RenderPdfPreviewAsync();
        }
        else if (item.IsImage)
        {
            _previewPageCount = 1;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(item.Path);
                bmp.DecodePixelWidth = 230;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                PreviewImage.Source = bmp;
                PreviewInfoText.Text = Path.GetFileName(item.Path);
            }
            catch
            {
                PreviewImage.Source = null;
                PreviewInfoText.Text = "Cannot load image.";
            }

            PrevPageBtn.IsEnabled = false;
            NextPageBtn.IsEnabled = false;
            PageNavLabel.Text = "";
        }
    }

    private async Task RenderPdfPreviewAsync()
    {
        if (_previewFilePath == null) return;

        PreviewInfoText.Text = "Loading…";
        var bmp = await PreviewService.RenderPageAsync(_previewFilePath, _currentPreviewPage, 230);
        PreviewImage.Source = bmp;

        PreviewInfoText.Text = $"Page {_currentPreviewPage + 1} of {_previewPageCount}";
        PageNavLabel.Text = $"{_currentPreviewPage + 1}/{_previewPageCount}";
        PrevPageBtn.IsEnabled = _currentPreviewPage > 0;
        NextPageBtn.IsEnabled = _currentPreviewPage < _previewPageCount - 1;
    }

    private void ClearPreview()
    {
        PreviewImage.Source = null;
        PreviewInfoText.Text = "No file selected";
        PageNavLabel.Text = "";
        PrevPageBtn.IsEnabled = false;
        NextPageBtn.IsEnabled = false;
        _previewFilePath = null;
        _previewPageCount = 0;
        _currentPreviewPage = 0;
    }

    private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPreviewPage > 0)
        {
            _currentPreviewPage--;
            _ = RenderPdfPreviewAsync();
        }
    }

    private void NextPageBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPreviewPage < _previewPageCount - 1)
        {
            _currentPreviewPage++;
            _ = RenderPdfPreviewAsync();
        }
    }

    // ─── Merge ────────────────────────────────────────────────────

    private void MergeFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            MessageBox.Show("Please add at least one file to the queue.", "No Files", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = SettingsService.Current;

        var dlg = new SaveFileDialog
        {
            Title = "Save Merged PDF",
            Filter = "PDF Files|*.pdf",
            FileName = settings.DefaultMergeFilename,
            InitialDirectory = string.IsNullOrWhiteSpace(settings.DefaultOutputFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : settings.DefaultOutputFolder
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            StatusChanged?.Invoke(this, "Merging files…");
            PdfService.MergeFiles(_files, dlg.FileName);
            StatusChanged?.Invoke(this, $"Merged successfully: {Path.GetFileName(dlg.FileName)}");

            MessageBox.Show($"Files merged successfully!\n\nSaved to:\n{dlg.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            if (settings.AutoOpenFolder)
            {
                var folder = Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(folder))
                    Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Merge failed.");
            MessageBox.Show($"Error merging files:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
