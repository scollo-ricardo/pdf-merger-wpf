namespace PDFMerger.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Light";
    public string DefaultOutputFolder { get; set; } = "";
    public string DefaultMergeFilename { get; set; } = "merged";
    public bool AutoOpenFolder { get; set; } = true;
}
