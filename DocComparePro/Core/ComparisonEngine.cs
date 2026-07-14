using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DocComparePro.Core;

public interface IComparisonEngine
{
    ComparisonResult Compare(string leftText, string rightText, ComparisonOptions options);
}

public sealed class ComparisonEngine : IComparisonEngine
{
    private static readonly Regex WordTokenizer =
        new(@"\p{L}+[\p{L}\p{M}'’-]*|\p{N}+(?:[.,]\p{N}+)*|[^\s]", RegexOptions.Compiled);

    private static readonly Regex SentenceTokenizer =
        new(@"(?<=[.!?])\s+|\r?\n+", RegexOptions.Compiled);

    public ComparisonResult Compare(string leftText, string rightText, ComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(leftText);
        ArgumentNullException.ThrowIfNull(rightText);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        var leftUnits = Tokenize(leftText, options);
        var rightUnits = Tokenize(rightText, options);
        var differences = BuildDiff(leftUnits, rightUnits, options);
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
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .ToArray();
        }

        var values = WordTokenizer.Matches(text)
            .Select(match => match.Value)
            .Where(value => options.ComparePunctuation || value.Any(char.IsLetterOrDigit))
            .Where(value => options.CompareNumbers || !value.All(character => char.IsDigit(character) || character is '.' or ','))
            .ToArray();

        return options.IgnoreWhitespace
            ? values
            : values;
    }

    // A longest-common-subsequence table produces a stable, deterministic diff
    // without mutating the original document content.
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
        var i = 0;
        var j = 0;
        var position = 1;

        while (i < left.Count && j < right.Count)
        {
            if (AreEqual(left[i], right[j], options))
            {
                result.Add(new DifferenceItem(DifferenceKind.Equal, left[i], right[j], position++));
                i++;
                j++;
            }
            else if (table[i + 1, j] >= table[i, j + 1])
            {
                result.Add(new DifferenceItem(DifferenceKind.Removed, left[i], string.Empty, position++));
                i++;
            }
            else
            {
                result.Add(new DifferenceItem(DifferenceKind.Added, string.Empty, right[j], position++));
                j++;
            }
        }

        while (i < left.Count)
        {
            result.Add(new DifferenceItem(DifferenceKind.Removed, left[i++], string.Empty, position++));
        }

        while (j < right.Count)
        {
            result.Add(new DifferenceItem(DifferenceKind.Added, string.Empty, right[j++], position++));
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
        var normalized = options.IgnoreWhitespace
            ? Regex.Replace(value, @"\s+", " ").Trim()
            : value;

        if (!options.ComparePunctuation)
        {
            normalized = new string(normalized.Where(character => !char.IsPunctuation(character)).ToArray());
        }

        if (!options.CompareNumbers)
        {
            normalized = Regex.Replace(normalized, @"\d", "#");
        }

        return normalized;
    }

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
                _ => "  "
            };

            builder.Append(marker).AppendLine(value);
        }

        return builder.ToString();
    }
}
