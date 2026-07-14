namespace DocComparePro.Core;

public enum ComparisonMode
{
    Words,
    Sentences
}

public enum DifferenceKind
{
    Equal,
    Added,
    Removed
}

public sealed record ComparisonOptions(
    ComparisonMode Mode,
    bool CaseSensitive,
    bool CompareNumbers,
    bool ComparePunctuation,
    bool IgnoreWhitespace,
    bool EnableOcr);

public sealed record DifferenceItem(
    DifferenceKind Kind,
    string LeftText,
    string RightText,
    int Position);

public sealed record DocumentContent(
    string FilePath,
    string DisplayName,
    string Text,
    long SizeInBytes);

public sealed record ComparisonResult(
    IReadOnlyList<DifferenceItem> Differences,
    string LeftPreview,
    string RightPreview,
    double SimilarityPercentage,
    int DifferenceCount,
    int ComparedUnitCount,
    TimeSpan ProcessingTime);
