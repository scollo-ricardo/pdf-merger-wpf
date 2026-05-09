using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using PDFMerger.Models;

namespace PDFMerger.Services;

public static class PdfService
{
    /// <summary>
    /// Merge PDFs and images into one PDF output file.
    /// </summary>
    public static void MergeFiles(IEnumerable<FileItem> files, string outputPath)
    {
        var tempFiles = new List<string>();
        try
        {
            using var writer = new PdfWriter(outputPath);
            using var outputDoc = new PdfDocument(writer);

            foreach (var file in files)
            {
                string pdfPath = file.Path;

                if (file.IsImage)
                {
                    pdfPath = Path.GetTempFileName() + ".pdf";
                    tempFiles.Add(pdfPath);
                    ConvertImageToPdf(file.Path, pdfPath);
                }

                using var reader = new PdfReader(pdfPath);
                using var srcDoc = new PdfDocument(reader);
                srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), outputDoc);
            }
        }
        finally
        {
            foreach (var t in tempFiles)
                try { File.Delete(t); } catch { }
        }
    }

    private static void ConvertImageToPdf(string imagePath, string outputPath)
    {
        var imageData = ImageDataFactory.Create(imagePath);
        var image = new Image(imageData);
        float w = image.GetImageWidth();
        float h = image.GetImageHeight();
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(writer);
        pdfDoc.SetDefaultPageSize(new PageSize(w, h));
        using var doc = new Document(pdfDoc);
        doc.SetMargins(0, 0, 0, 0);
        image.SetFixedPosition(0, 0);
        image.SetWidth(w);
        image.SetHeight(h);
        doc.Add(image);
    }

    /// <summary>
    /// Split a PDF into multiple output PDFs by page assignment.
    /// </summary>
    public static void SplitPdf(string sourcePath, List<(string Name, List<int> Pages)> outputs, string outputDir, string baseName)
    {
        using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var srcPdf = new PdfDocument(new PdfReader(srcStream));

        foreach (var (name, pages) in outputs)
        {
            if (pages.Count == 0) continue;

            var sanitizedName = SanitizeFileName(name);
            var outputPath = System.IO.Path.Combine(outputDir, $"{baseName}_{sanitizedName}.pdf");

            using var outPdf = new PdfDocument(new PdfWriter(outputPath));

            var sortedPages = pages.OrderBy(p => p).ToList();
            foreach (var pageNum in sortedPages)
            {
                if (pageNum >= 1 && pageNum <= srcPdf.GetNumberOfPages())
                    srcPdf.CopyPagesTo(pageNum, pageNum, outPdf);
            }
        }
    }

    /// <summary>
    /// Rotate pages in a PDF. rotations maps 0-based page index to cumulative angle (multiple of 90).
    /// </summary>
    public static void RotatePages(string sourcePath, Dictionary<int, int> rotations, string outputPath)
    {
        using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        using var pdfDoc = new PdfDocument(new PdfReader(srcStream), new PdfWriter(outStream));

        int pageCount = pdfDoc.GetNumberOfPages();
        for (int i = 0; i < pageCount; i++)
        {
            if (rotations.TryGetValue(i, out int angle))
            {
                var page = pdfDoc.GetPage(i + 1);
                // Normalize angle to 0, 90, 180, 270
                int normalized = ((angle % 360) + 360) % 360;
                int existing = page.GetRotation();
                int newRotation = (existing + normalized) % 360;
                page.SetRotation(newRotation);
            }
        }
    }

    /// <summary>
    /// Compress a PDF. level = "Low" / "Medium" / "High".
    /// </summary>
    public static void CompressPdf(string sourcePath, string outputPath, string level)
    {
        var writerProps = new WriterProperties();

        switch (level)
        {
            case "High":
                writerProps.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
                writerProps.UseSmartMode();
                break;
            case "Medium":
                writerProps.SetCompressionLevel(CompressionConstants.DEFAULT_COMPRESSION);
                writerProps.UseSmartMode();
                break;
            case "Low":
            default:
                writerProps.SetCompressionLevel(CompressionConstants.BEST_SPEED);
                break;
        }

        using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        using var pdfDoc = new PdfDocument(new PdfReader(srcStream), new PdfWriter(outStream, writerProps));
        // Simply opening and writing re-compresses the PDF
    }

    /// <summary>
    /// Returns the page count of a PDF.
    /// </summary>
    public static int GetPageCount(string pdfPath)
    {
        try
        {
            using var stream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read);
            using var pdfDoc = new PdfDocument(new PdfReader(stream));
            return pdfDoc.GetNumberOfPages();
        }
        catch
        {
            return 0;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "output" : sanitized;
    }
}
