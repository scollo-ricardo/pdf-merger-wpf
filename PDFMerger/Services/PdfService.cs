using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Kernel.Geom;
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
        using var writerStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var pdfWriter = new PdfWriter(writerStream);
        using var pdfDoc = new PdfDocument(pdfWriter);
        using var document = new Document(pdfDoc);

        document.SetMargins(0, 0, 0, 0);

        bool firstPage = true;

        foreach (var file in files)
        {
            if (file.IsPdf)
            {
                using var srcStream = new FileStream(file.Path, FileMode.Open, FileAccess.Read);
                using var srcPdf = new PdfDocument(new PdfReader(srcStream));

                var merger = new PdfMerger(pdfDoc);
                merger.Merge(srcPdf, 1, srcPdf.GetNumberOfPages());
            }
            else if (file.IsImage)
            {
                var imageData = ImageDataFactory.Create(file.Path);
                var image = new Image(imageData);

                float imgWidth = image.GetImageWidth();
                float imgHeight = image.GetImageHeight();

                // Scale down if too large (max A4 at 72dpi: 595x842 points)
                float maxWidth = 595f;
                float maxHeight = 842f;
                float scaleX = imgWidth > maxWidth ? maxWidth / imgWidth : 1f;
                float scaleY = imgHeight > maxHeight ? maxHeight / imgHeight : 1f;
                float scale = Math.Min(scaleX, scaleY);

                float pageWidth = imgWidth * scale;
                float pageHeight = imgHeight * scale;

                if (!firstPage)
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                pdfDoc.SetDefaultPageSize(new PageSize(pageWidth, pageHeight));

                image.ScaleToFit(pageWidth, pageHeight);
                image.SetFixedPosition(0, 0);
                document.Add(image);
            }

            firstPage = false;
        }
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

            using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var outPdf = new PdfDocument(new PdfWriter(outStream));

            var merger = new PdfMerger(outPdf);

            // Sort pages and merge them in order
            var sortedPages = pages.OrderBy(p => p).ToList();
            foreach (var pageNum in sortedPages)
            {
                if (pageNum >= 1 && pageNum <= srcPdf.GetNumberOfPages())
                {
                    merger.Merge(srcPdf, pageNum, pageNum);
                }
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
