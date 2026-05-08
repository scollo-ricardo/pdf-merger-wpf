namespace PDFMerger.Models;

public class FileItem
{
    public string Path { get; }
    public string DisplayName { get; }
    public bool IsPdf => Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    private static readonly string[] ImageExts = [".png", ".jpg", ".jpeg", ".bmp", ".tiff"];
    public bool IsImage => ImageExts.Contains(System.IO.Path.GetExtension(Path).ToLowerInvariant());

    public FileItem(string path)
    {
        Path = path;
        var tag = IsPdf ? "[PDF]" : "[IMG]";
        DisplayName = $"{tag}  {System.IO.Path.GetFileName(path)}";
    }

    public override string ToString() => DisplayName;
}
