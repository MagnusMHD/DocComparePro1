using System.Text;
using Tesseract;
using UglyToad.PdfPig;

namespace DocComparePro.Core;

public interface IDocumentReader
{
    Task<DocumentContent> ReadAsync(string filePath, bool enableOcr, CancellationToken cancellationToken = default);
}

public sealed class DocumentReader : IDocumentReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".pdf", ".png", ".jpg", ".jpeg" };

    public async Task<DocumentContent> ReadAsync(
        string filePath,
        bool enableOcr,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Die ausgewählte Datei wurde nicht gefunden.", filePath);
        }

        var extension = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException($"Der Dateityp '{extension}' wird nicht unterstützt.");
        }

        var text = extension.ToLowerInvariant() switch
        {
            ".txt" => await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken),
            ".pdf" => await Task.Run(() => ReadPdf(filePath), cancellationToken),
            ".png" or ".jpg" or ".jpeg" when enableOcr =>
                await Task.Run(() => ReadImageWithOcr(filePath), cancellationToken),
            ".png" or ".jpg" or ".jpeg" =>
                throw new InvalidOperationException("Für Bilddateien muss OCR aktiviert sein."),
            _ => throw new NotSupportedException()
        };

        var info = new FileInfo(filePath);
        return new DocumentContent(filePath, info.Name, text, info.Length);
    }

    private static string ReadPdf(string filePath)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ReadImageWithOcr(string filePath)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!Directory.Exists(dataPath))
        {
            throw new DirectoryNotFoundException(
                "Der OCR-Ordner 'tessdata' fehlt. Lege dort mindestens eng.traineddata oder deu.traineddata ab.");
        }

        using var engine = new TesseractEngine(dataPath, "deu+eng", EngineMode.Default);
        using var image = Pix.LoadFromFile(filePath);
        using var page = engine.Process(image);
        return page.GetText();
    }
}
