using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace DocComparePro.Core;

/// <summary>
/// Writes comparison results to a portable report file.
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// Exports a result based on the target file extension.
    /// </summary>
    Task ExportAsync(
        string filePath,
        string leftDocumentName,
        string rightDocumentName,
        ComparisonResult result,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supports HTML and CSV reports without additional runtime dependencies.
/// </summary>
public sealed class ReportExporter : IReportExporter
{
    /// <inheritdoc />
    public async Task ExportAsync(
        string filePath,
        string leftDocumentName,
        string rightDocumentName,
        ComparisonResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(result);

        var content = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".html" or ".htm" => BuildHtml(leftDocumentName, rightDocumentName, result),
            ".csv" => BuildCsv(result),
            _ => throw new NotSupportedException("Es werden nur HTML- und CSV-Berichte unterstützt.")
        };

        await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(true), cancellationToken);
    }

    private static string BuildHtml(
        string leftDocumentName,
        string rightDocumentName,
        ComparisonResult result)
    {
        var rows = new StringBuilder();
        foreach (var difference in result.Differences.Where(item => item.Kind != DifferenceKind.Equal))
        {
            rows.Append("<tr><td>")
                .Append(difference.Position)
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(difference.Kind.ToString()))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(difference.LeftText))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(difference.RightText))
                .AppendLine("</td></tr>");
        }

        return $$"""
<!doctype html>
<html lang="de">
<head>
<meta charset="utf-8">
<title>DocComparePro Bericht</title>
<style>
body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#1b2430;background:#f6f8fb}
main{max-width:1200px;margin:auto;background:white;padding:28px;border-radius:14px;box-shadow:0 8px 30px #0001}
h1{margin-top:0}.stats{display:flex;gap:18px;flex-wrap:wrap}.stat{padding:14px 18px;background:#eef3ff;border-radius:10px}
table{width:100%;border-collapse:collapse;margin-top:24px}th,td{padding:10px;border-bottom:1px solid #dbe2ef;text-align:left;vertical-align:top}th{background:#13213a;color:white}
</style>
</head>
<body><main>
<h1>DocComparePro Vergleichsbericht</h1>
<p><strong>Dokument A:</strong> {{WebUtility.HtmlEncode(leftDocumentName)}}<br><strong>Dokument B:</strong> {{WebUtility.HtmlEncode(rightDocumentName)}}</p>
<div class="stats"><div class="stat">Ähnlichkeit: <strong>{{result.SimilarityPercentage.ToString("N2", CultureInfo.GetCultureInfo("de-DE"))}} %</strong></div><div class="stat">Unterschiede: <strong>{{result.DifferenceCount}}</strong></div><div class="stat">Einheiten: <strong>{{result.ComparedUnitCount}}</strong></div><div class="stat">Dauer: <strong>{{result.ProcessingTime.TotalMilliseconds:N0}} ms</strong></div></div>
<table><thead><tr><th>#</th><th>Typ</th><th>Dokument A</th><th>Dokument B</th></tr></thead><tbody>
{{rows}}
</tbody></table>
</main></body></html>
""";
    }

    private static string BuildCsv(ComparisonResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Position;Typ;Dokument A;Dokument B");

        foreach (var difference in result.Differences.Where(item => item.Kind != DifferenceKind.Equal))
        {
            builder.Append(difference.Position).Append(';')
                .Append(EscapeCsv(difference.Kind.ToString())).Append(';')
                .Append(EscapeCsv(difference.LeftText)).Append(';')
                .Append(EscapeCsv(difference.RightText)).AppendLine();
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}