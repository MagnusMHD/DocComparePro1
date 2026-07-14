namespace DocComparePro.Core;

/// <summary>
/// Defines how a document is split before comparison.
/// </summary>
public enum ComparisonMode
{
    /// <summary>Compares individual words, numbers and optional punctuation.</summary>
    Words,

    /// <summary>Compares complete sentences.</summary>
    Sentences
}

/// <summary>
/// Describes the relationship of one comparison unit between both documents.
/// </summary>
public enum DifferenceKind
{
    /// <summary>The unit exists unchanged in both documents.</summary>
    Equal,

    /// <summary>The unit exists only in the right document.</summary>
    Added,

    /// <summary>The unit exists only in the left document.</summary>
    Removed,

    /// <summary>A unit in the left document was replaced by another unit.</summary>
    Changed
}

/// <summary>
/// Contains all user-selectable comparison settings.
/// </summary>
public sealed record ComparisonOptions(
    ComparisonMode Mode,
    bool CaseSensitive,
    bool CompareNumbers,
    bool ComparePunctuation,
    bool IgnoreWhitespace,
    bool EnableOcr);

/// <summary>
/// Represents one aligned comparison unit.
/// </summary>
public sealed record DifferenceItem(
    DifferenceKind Kind,
    string LeftText,
    string RightText,
    int Position);

/// <summary>
/// Contains extracted text and file metadata.
/// </summary>
public sealed record DocumentContent(
    string FilePath,
    string DisplayName,
    string Text,
    long SizeInBytes);

/// <summary>
/// Contains the complete result of one comparison operation.
/// </summary>
public sealed record ComparisonResult(
    IReadOnlyList<DifferenceItem> Differences,
    string LeftPreview,
    string RightPreview,
    double SimilarityPercentage,
    int DifferenceCount,
    int ComparedUnitCount,
    TimeSpan ProcessingTime);