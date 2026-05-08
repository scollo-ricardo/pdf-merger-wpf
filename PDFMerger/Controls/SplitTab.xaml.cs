using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using PDFMerger.Services;

namespace PDFMerger.Controls;

public partial class SplitTab : UserControl
{
    public event EventHandler<string>? StatusChanged;

    private string? _pdfPath;
    private int _pageCount;
    private int _columnCount = 2;

    // Column data: index -> list of 1-based page numbers
    private readonly List<List<int>> _columnPages = new();
    private List<int> _sourcePages = new();

    public SplitTab()
    {
        InitializeComponent();
    }

    // ─── File loading ─────────────────────────────────────────────

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

        // Reset source pages
        _sourcePages = Enumerable.Range(1, _pageCount).ToList();

        RebuildColumns();
        StatusChanged?.Invoke(this, $"Loaded: {Path.GetFileName(path)} — {_pageCount} pages.");
    }

    // ─── Column management ────────────────────────────────────────

    private void DecrColumns_Click(object sender, RoutedEventArgs e)
    {
        if (_columnCount > 1)
        {
            _columnCount--;
            ColumnCountLabel.Text = _columnCount.ToString();
            RebuildColumns();
        }
    }

    private void IncrColumns_Click(object sender, RoutedEventArgs e)
    {
        if (_columnCount < 10)
        {
            _columnCount++;
            ColumnCountLabel.Text = _columnCount.ToString();
            RebuildColumns();
        }
    }

    private void RebuildColumns()
    {
        ColumnsPanel.Children.Clear();
        _columnPages.Clear();

        // Source column
        var sourceColumn = BuildColumn("Source Pages", null, isSource: true);
        ColumnsPanel.Children.Add(sourceColumn.border);

        // Output columns
        for (int i = 0; i < _columnCount; i++)
        {
            _columnPages.Add(new List<int>());
            var outColumn = BuildColumn($"Output {i + 1}", i, isSource: false);
            ColumnsPanel.Children.Add(outColumn.border);
        }

        RefreshSourceList();
    }

    private (Border border, ListBox listBox, TextBox nameBox) BuildColumn(string defaultName, int? colIndex, bool isSource)
    {
        var border = new Border
        {
            Width = 160,
            Margin = new Thickness(0, 0, 6, 0),
            BorderBrush = SystemColors.ActiveBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };

        var sp = new StackPanel { Margin = new Thickness(4) };

        TextBox? nameBox = null;
        if (isSource)
        {
            var label = new TextBlock
            {
                Text = "Source Pages",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(label);
        }
        else
        {
            nameBox = new TextBox
            {
                Text = defaultName,
                Margin = new Thickness(0, 0, 0, 4),
                Tag = colIndex
            };
            sp.Children.Add(nameBox);
        }

        var listBox = new ListBox
        {
            Height = 340,
            AllowDrop = true,
            Tag = colIndex,
            SelectionMode = SelectionMode.Single
        };

        listBox.PreviewMouseLeftButtonDown += PageList_MouseDown;
        listBox.Drop += PageList_Drop;
        listBox.DragOver += PageList_DragOver;
        listBox.SelectionChanged += PageList_SelectionChanged;

        sp.Children.Add(listBox);
        border.Child = sp;

        // Store references so we can find them later
        border.Tag = colIndex;

        return (border, listBox, nameBox!);
    }

    private void RefreshSourceList()
    {
        if (ColumnsPanel.Children.Count == 0) return;

        var sourceBorder = ColumnsPanel.Children[0] as Border;
        if (sourceBorder == null) return;

        var sp = sourceBorder.Child as StackPanel;
        if (sp == null) return;

        var listBox = sp.Children.OfType<ListBox>().FirstOrDefault();
        if (listBox == null) return;

        listBox.Items.Clear();

        // Show pages not yet assigned to output columns
        var assignedPages = _columnPages.SelectMany(c => c).ToHashSet();
        foreach (var page in _sourcePages)
        {
            if (!assignedPages.Contains(page))
                listBox.Items.Add($"Page {page}");
        }
    }

    private ListBox? GetOutputListBox(int colIndex)
    {
        // Index 0 is source, output columns start at 1
        if (colIndex + 1 >= ColumnsPanel.Children.Count) return null;

        var border = ColumnsPanel.Children[colIndex + 1] as Border;
        var sp = border?.Child as StackPanel;
        return sp?.Children.OfType<ListBox>().FirstOrDefault();
    }

    private TextBox? GetOutputNameBox(int colIndex)
    {
        if (colIndex + 1 >= ColumnsPanel.Children.Count) return null;

        var border = ColumnsPanel.Children[colIndex + 1] as Border;
        var sp = border?.Child as StackPanel;
        return sp?.Children.OfType<TextBox>().FirstOrDefault();
    }

    // ─── Drag-drop between columns ────────────────────────────────

    private ListBox? _dragSourceListBox;
    private string? _draggedPageLabel;

    private void PageList_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is string pageLabel)
        {
            _dragSourceListBox = lb;
            _draggedPageLabel = pageLabel;
            DragDrop.DoDragDrop(lb, pageLabel, DragDropEffects.Move);
        }
    }

    private void PageList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void PageList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;

        var pageLabel = (string)e.Data.GetData(DataFormats.StringFormat);
        if (!pageLabel.StartsWith("Page ")) return;

        if (!int.TryParse(pageLabel.Substring(5), out int pageNum)) return;

        var targetListBox = sender as ListBox;
        if (targetListBox == null || targetListBox == _dragSourceListBox) return;

        int? targetColIndex = targetListBox.Tag as int?;

        // Remove from source column (if it's the output assignment)
        if (_dragSourceListBox?.Tag is int sourceColIndex)
        {
            _columnPages[sourceColIndex].Remove(pageNum);
            _dragSourceListBox.Items.Remove(pageLabel);
        }
        else
        {
            // Dragging from source column; source column items are unassigned
        }

        // Add to target
        if (targetColIndex.HasValue)
        {
            // Dropping onto output column
            if (!_columnPages[targetColIndex.Value].Contains(pageNum))
            {
                _columnPages[targetColIndex.Value].Add(pageNum);
                targetListBox.Items.Add(pageLabel);
            }
        }
        else
        {
            // Dropping back onto source column — already handled by removing from output
        }

        RefreshSourceList();
        e.Handled = true;
    }

    private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is string pageLabel)
        {
            if (pageLabel.StartsWith("Page ") && int.TryParse(pageLabel.Substring(5), out int pageNum))
                _ = ShowPreviewAsync(pageNum - 1);
        }
    }

    private async Task ShowPreviewAsync(int pageIndex)
    {
        if (_pdfPath == null) return;

        PreviewInfoText.Text = "Loading…";
        var bmp = await PreviewService.RenderPageAsync(_pdfPath, pageIndex, 260);
        PreviewImage.Source = bmp;
        PreviewInfoText.Text = $"Page {pageIndex + 1}";
    }

    // ─── Split & Save ─────────────────────────────────────────────

    private void SplitSave_Click(object sender, RoutedEventArgs e)
    {
        if (_pdfPath == null)
        {
            MessageBox.Show("Please load a PDF first.", "No File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Collect outputs from each column
        var outputs = new List<(string Name, List<int> Pages)>();
        for (int i = 0; i < _columnCount; i++)
        {
            var nameBox = GetOutputNameBox(i);
            var name = nameBox?.Text ?? $"Output {i + 1}";
            var pages = new List<int>(_columnPages[i]);
            outputs.Add((name, pages));
        }

        if (outputs.All(o => o.Pages.Count == 0))
        {
            MessageBox.Show("Please assign at least one page to an output column.", "No Pages Assigned", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var folderDlg = new OpenFolderDialog
        {
            Title = "Select Output Folder"
        };

        if (folderDlg.ShowDialog() != true) return;

        try
        {
            StatusChanged?.Invoke(this, "Splitting PDF…");
            var baseName = Path.GetFileNameWithoutExtension(_pdfPath);
            PdfService.SplitPdf(_pdfPath, outputs, folderDlg.FolderName, baseName);
            StatusChanged?.Invoke(this, "Split completed.");
            MessageBox.Show("PDF split successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Split failed.");
            MessageBox.Show($"Error splitting PDF:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
