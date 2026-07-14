using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DocComparePro.Core;

/// <summary>
/// Compares two extracted document texts.
/// </summary>
public interface IComparisonEngine
{
    /// <summary>
    /// Compares two texts using the supplied options.
    /// </summary>
    ComparisonResult Compare(
        string leftText,
        string rightText,
        ComparisonOptions options,
        CancellationToken cancellationToken = default,
        IProgress<int>? progress = null);
}

/// <summary>
/// Creates deterministic word or sentence differences using a longest-common-subsequence algorithm.
/// </summary>
public sealed class ComparisonEngine : IComparisonEngine
{
    private const double ReplacementThreshold = 0.35;

    private static readonly Regex WordTokenizer =
        new(@"\p{L}+[\p{L}\p{M}'’-]*|\p{N}+(?:[.,]\p{N}+)*|[^\s]", RegexOptions.Compiled);

    private static readonly Regex SentenceTokenizer =
        new(@"(?<=[.!?])\s+|\r?\n+", RegexOptions.Compiled);

    /// <inheritdoc />
    public ComparisonResult Compare(
        string leftText,
        string rightText,
        ComparisonOptions options,
        CancellationToken cancellationToken = default,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(leftText);
        ArgumentNullException.ThrowIfNull(rightText);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        progress?.Report(5);

        var leftUnits = Tokenize(leftText, options);
        var rightUnits = Tokenize(rightText, options);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(15);

        var rawDifferences = BuildDiff(leftUnits, rightUnits, options, cancellationToken, progress);
        var differences = CombineReplacements(rawDifferences, options);
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(90);

        var comparedUnitCount = Math.Max(leftUnits.Count, rightUnits.Count);
        var similarity = CalculateSimilarity(differences, comparedUnitCount, options);
        stopwatch.Stop();
        progress?.Report(100);

        return new ComparisonResult(
            differences,
            BuildPreview(differences, useLeftSide: true),
            BuildPreview(differences, useLeftSide: false),
            similarity,
            differences.Count(item => item.Kind != DifferenceKind.Equal),
            comparedUnitCount,
            stopwatch.Elapsed);
    }

    private static IReadOnlyList<string> Tokenize(string text, ComparisonOptions options)
    {
        if (options.Mode == ComparisonMode.Sentences)
        {
            return SentenceTokenizer.Split(text)
                .Select(value => NormalizeWhitespace(value, options.IgnoreWhitespace))
                .Where(value => value.Length > 0)
                .ToArray();
        }

        return WordTokenizer.Matches(text)
            .Select(match => match.Value)
            .Where(value => options.ComparePunctuation || value.Any(char.IsLetterOrDigit))
            .ToArray();
    }

    // The LCS matrix produces stable output and preserves the original sequence.
    private static IReadOnlyList<DifferenceItem> BuildDiff(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right,
        ComparisonOptions options,
        CancellationToken cancellationToken,
        IProgress<int>? progress)
    {
        var table = new int[left.Count + 1, right.Count + 1];
        var totalRows = Math.Max(left.Count, 1);

        for (var leftIndex = left.Count - 1; leftIndex >= 0; leftIndex--)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var rightIndex = right.Count - 1; rightIndex >= 0; rightIndex--)
            {
                table[leftIndex, rightIndex] = AreEqual(left[leftIndex], right[rightIndex], options)
                    ? table[leftIndex + 1, rightIndex + 1] + 1
                    : Math.Max(table[leftIndex + 1, rightIndex], table[leftIndex, rightIndex + 1]);
            }

            var completedRows = left.Count - leftIndex;
            progress?.Report(15 + (int)(completedRows * 60d / totalRows));
        }

        var result = new List<DifferenceItem>();
        var leftPosition = 0;
        var rightPosition = 0;
        var displayPosition = 1;

        while (leftPosition < left.Count && rightPosition < right.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (AreEqual(left[leftPosition], right[rightPosition], options))
            {
                result.Add(new DifferenceItem(
                    DifferenceKind.Equal,
                    left[leftPosition++],
                    right[rightPosition++],
                    displayPosition++));
            }
            else if (table[leftPosition + 1, rightPosition] >= table[leftPosition, rightPosition + 1])
            {
                result.Add(new DifferenceItem(
                    DifferenceKind.Removed,
                    left[leftPosition++],
                    string.Empty,
                    displayPosition++));
            }
            else
            {
                result.Add(new DifferenceItem(
                    DifferenceKind.Added,
                    string.Empty,
                    right[rightPosition++],
                    displayPosition++));
            }
        }

        while (leftPosition < left.Count)
        {
            result.Add(new DifferenceItem(
                DifferenceKind.Removed,
                left[leftPosition++],
                string.Empty,
                displayPosition++));
        }

        while (rightPosition < right.Count)
        {
            result.Add(new DifferenceItem(
                DifferenceKind.Added,
                string.Empty,
                right[rightPosition++],
                displayPosition++));
        }

        return result;
    }

    // Adjacent remove/add pairs become one replacement when both values are sufficiently related.
    private static IReadOnlyList<DifferenceItem> CombineReplacements(
        IReadOnlyList<DifferenceItem> source,
        ComparisonOptions options)
    {
        var result = new List<DifferenceItem>(source.Count);

        for (var index = 0; index < source.Count; index++)
        {
            var current = source[index];
            if (current.Kind == DifferenceKind.Removed &&
                index + 1 < source.Count &&
                source[index + 1].Kind == DifferenceKind.Added)
            {
                var added = source[index + 1];
                var similarity = CalculateUnitSimilarity(current.LeftText, added.RightText, options);
                if (similarity >= ReplacementThreshold)
                {
                    index++;
                    result.Add(new DifferenceItem(
                        DifferenceKind.Changed,
                        current.LeftText,
                        added.RightText,
                        current.Position));
                    continue;
                }
            }

            result.Add(current);
        }

        return result;
    }

    private static double CalculateSimilarity(
        IReadOnlyList<DifferenceItem> differences,
        int comparedUnitCount,
        ComparisonOptions options)
    {
        if (comparedUnitCount == 0)
        {
            return 100d;
        }

        var score = differences.Sum(item => item.Kind switch
        {
            DifferenceKind.Equal => 1d,
            DifferenceKind.Changed => CalculateUnitSimilarity(item.LeftText, item.RightText, options),
            _ => 0d
        });

        return Math.Round(Math.Min(100d, score * 100d / comparedUnitCount), 2);
    }

    private static double CalculateUnitSimilarity(string left, string right, ComparisonOptions options)
    {
        var normalizedLeft = Normalize(left, options);
        var normalizedRight = Normalize(right, options);
        var longestLength = Math.Max(normalizedLeft.Length, normalizedRight.Length);

        if (longestLength == 0)
        {
            return 1d;
        }

        var distance = CalculateLevenshteinDistance(normalizedLeft, normalizedRight);
        return 1d - distance / (double)longestLength;
    }

    private static int CalculateLevenshteinDistance(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;

            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static bool AreEqual(string left, string right, ComparisonOptions options)
    {
        var comparison = options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return string.Equals(Normalize(left, options), Normalize(right, options), comparison);
    }

    private static string Normalize(string value, ComparisonOptions options)
    {
        var normalized = NormalizeWhitespace(value, options.IgnoreWhitespace);

        if (!options.ComparePunctuation)
        {
            normalized = new string(normalized.Where(character => !char.IsPunctuation(character)).ToArray());
        }

        if (!options.CompareNumbers)
        {
            normalized = Regex.Replace(normalized, @"\d", string.Empty);
        }

        return options.CaseSensitive ? normalized : normalized.ToUpperInvariant();
    }

    private static string NormalizeWhitespace(string value, bool normalize) =>
        normalize ? Regex.Replace(value, @"\s+", " ").Trim() : value.Trim();

    private static string BuildPreview(IReadOnlyList<DifferenceItem> differences, bool useLeftSide)
    {
        var builder = new StringBuilder();

        foreach (var difference in differences)
        {
            var value = useLeftSide ? difference.LeftText : difference.RightText;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var marker = difference.Kind switch
            {
                DifferenceKind.Equal => "  ",
                DifferenceKind.Removed when useLeftSide => "- ",
                DifferenceKind.Added when !useLeftSide => "+ ",
                DifferenceKind.Changed => "~ ",
                _ => "  "
            };

            builder.Append(marker).AppendLine(value);
        }

        return builder.ToString();
    }
}
