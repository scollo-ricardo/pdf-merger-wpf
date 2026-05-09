namespace PDFMerger.Models;

public class FileItem
{
    public string Path { get; }
    public string DisplayName { get; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Tag => IsPdf ? "PDF" : "IMG";
    public bool IsPdf => Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    private static readonly string[] ImageExts = [".png", ".jpg", ".jpeg", ".bmp", ".tiff"];
    public bool IsImage => ImageExts.Contains(System.IO.Path.GetExtension(Path).ToLowerInvariant());

    public FileItem(string path)
    {
        Path = path;
        DisplayName = System.IO.Path.GetFileName(path);
    }

    public override string ToString() => DisplayName;
}
