using System.IO;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PDFMerger.Services;

public static class PreviewService
{
    /// <summary>
    /// Renders a PDF page to a BitmapSource for WPF display.
    /// Returns null on failure.
    /// </summary>
    public static async Task<BitmapSource?> RenderPageAsync(string pdfPath, int pageIndex, int targetWidth = 400)
    {
        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
            var pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);

            if (pageIndex < 0 || pageIndex >= (int)pdfDocument.PageCount)
                return null;

            using var page = pdfDocument.GetPage((uint)pageIndex);
            using var stream = new InMemoryRandomAccessStream();

            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)targetWidth
            };

            await page.RenderToStreamAsync(stream, options);

            stream.Seek(0);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream.AsStream();
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts a PDF page as an image file (PNG or JPG).
    /// </summary>
    public static async Task ExtractPageAsImageAsync(string pdfPath, int pageIndex, string outputPath, string format, int dpi)
    {
        var storageFile = await StorageFile.GetFileFromPathAsync(pdfPath);
        var pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);

        if (pageIndex < 0 || pageIndex >= (int)pdfDocument.PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageIndex));

        using var page = pdfDocument.GetPage((uint)pageIndex);
        using var stream = new InMemoryRandomAccessStream();

        // Scale based on DPI (96 is the base DPI for WinRT rendering)
        double scale = dpi / 96.0;
        uint destWidth = (uint)(page.Size.Width * scale);

        var options = new PdfPageRenderOptions
        {
            DestinationWidth = destWidth
        };

        await page.RenderToStreamAsync(stream, options);

        if (format.Equals("PNG", StringComparison.OrdinalIgnoreCase))
        {
            // Copy stream directly as PNG
            stream.Seek(0);
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            await stream.AsStream().CopyToAsync(fileStream);
        }
        else
        {
            // Decode the PNG from the stream, then re-encode as JPEG
            stream.Seek(0);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream.AsStream();
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fileStream);
        }
    }
}
