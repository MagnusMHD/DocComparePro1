using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tesseract;
using UglyToad.PdfPig;

namespace DocComparePro.Core;

/// <summary>
/// Extracts comparable text from supported document formats.
/// </summary>
public interface IDocumentReader
{
    /// <summary>
    /// Reads a file asynchronously and returns its extracted text and metadata.
    /// </summary>
    Task<DocumentContent> ReadAsync(
        string filePath,
        bool enableOcr,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supports text, PDF, DOCX and image files.
/// </summary>
public sealed class DocumentReader : IDocumentReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".pdf", ".docx", ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"
        };

    /// <inheritdoc />
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

        cancellationToken.ThrowIfCancellationRequested();

        var text = extension.ToLowerInvariant() switch
        {
            ".txt" => await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken),
            ".pdf" => await Task.Run(() => ReadPdf(filePath), cancellationToken),
            ".docx" => await Task.Run(() => ReadDocx(filePath), cancellationToken),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff" when enableOcr =>
                await Task.Run(() => ReadImageWithOcr(filePath), cancellationToken),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff" =>
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

    private static string ReadDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body
            ?? throw new InvalidDataException("Das DOCX-Dokument enthält keinen lesbaren Inhalt.");

        // Paragraph boundaries are preserved so sentence mode remains meaningful.
        return string.Join(
            Environment.NewLine,
            body.Descendants<Paragraph>().Select(paragraph => paragraph.InnerText));
    }

    private static string ReadImageWithOcr(string filePath)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (!Directory.Exists(dataPath))
        {
            throw new DirectoryNotFoundException(
                "Der OCR-Ordner 'tessdata' fehlt. Lege dort deu.traineddata oder eng.traineddata ab.");
        }

        var availableLanguages = new[] { "deu", "eng" }
            .Where(language => File.Exists(Path.Combine(dataPath, $"{language}.traineddata")))
            .ToArray();

        if (availableLanguages.Length == 0)
        {
            throw new FileNotFoundException(
                "Für OCR wird mindestens tessdata/deu.traineddata oder tessdata/eng.traineddata benötigt.");
        }

        using var engine = new TesseractEngine(
            dataPath,
            string.Join('+', availableLanguages),
            Tesseract.EngineMode.Default);
        engine.SetVariable("preserve_interword_spaces", "1");

        using var image = Pix.LoadFromFile(filePath);
        using var page = engine.Process(image, PageSegMode.Auto);
        var text = page.GetText()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException(
                "Im Bild wurde kein lesbarer Text erkannt. Prüfe Bildqualität, Ausrichtung und OCR-Sprachdateien.");
        }

        return text;
    }
}
