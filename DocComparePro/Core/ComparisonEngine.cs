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
    ComparisonResult Compare(string leftText, string rightText, ComparisonOptions options);
}

/// <summary>
/// Creates deterministic word or sentence differences using a longest-common-subsequence algorithm.
/// </summary>
public sealed class ComparisonEngine : IComparisonEngine
{
    private static readonly Regex WordTokenizer =
        new(@"\p{L}+[\p{L}\p{M}'’-]*|\p{N}+(?:[.,]\p{N}+)*|[^\s]", RegexOptions.Compiled);

    private static readonly Regex SentenceTokenizer =
        new(@"(?<=[.!?])\s+|\r?\n+", RegexOptions.Compiled);

    /// <inheritdoc />
    public ComparisonResult Compare(string leftText, string rightText, ComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(leftText);
        ArgumentNullException.ThrowIfNull(rightText);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        var leftUnits = Tokenize(leftText, options);
        var rightUnits = Tokenize(rightText, options);
        var rawDifferences = BuildDiff(leftUnits, rightUnits, options);
        var differences = CombineReplacements(rawDifferences);
        stopwatch.Stop();

        var equalCount = differences.Count(item => item.Kind == DifferenceKind.Equal);
        var comparedUnitCount = Math.Max(leftUnits.Count, rightUnits.Count);
        var similarity = comparedUnitCount == 0
            ? 100d
            : Math.Round(equalCount * 100d / comparedUnitCount, 2);

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
        ComparisonOptions options)
    {
        var table = new int[left.Count + 1, right.Count + 1];

        for (var leftIndex = left.Count - 1; leftIndex >= 0; leftIndex--)
        {
            for (var rightIndex = right.Count - 1; rightIndex >= 0; rightIndex--)
            {
                table[leftIndex, rightIndex] = AreEqual(left[leftIndex], right[rightIndex], options)
                    ? table[leftIndex + 1, rightIndex + 1] + 1
                    : Math.Max(table[leftIndex + 1, rightIndex], table[leftIndex, rightIndex + 1]);
            }
        }

        var result = new List<DifferenceItem>();
        var leftPosition = 0;
        var rightPosition = 0;
        var displayPosition = 1;

        while (leftPosition < left.Count && rightPosition < right.Count)
        {
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

    // Adjacent remove/add pairs are easier for users to understand as one replacement.
    private static IReadOnlyList<DifferenceItem> CombineReplacements(IReadOnlyList<DifferenceItem> source)
    {
        var result = new List<DifferenceItem>(source.Count);

        for (var index = 0; index < source.Count; index++)
        {
            var current = source[index];
            if (current.Kind == DifferenceKind.Removed &&
                index + 1 < source.Count &&
                source[index + 1].Kind == DifferenceKind.Added)
            {
                var added = source[++index];
                result.Add(new DifferenceItem(
                    DifferenceKind.Changed,
                    current.LeftText,
                    added.RightText,
                    current.Position));
                continue;
            }

            result.Add(current);
        }

        return result;
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
            // Removing digits means values such as invoice numbers do not influence equality.
            normalized = Regex.Replace(normalized, @"\d", string.Empty);
        }

        return normalized;
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