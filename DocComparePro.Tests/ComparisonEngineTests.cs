using DocComparePro.Core;
using Xunit;

namespace DocComparePro.Tests;

/// <summary>
/// Verifies the deterministic behavior of the comparison engine.
/// </summary>
public sealed class ComparisonEngineTests
{
    private readonly ComparisonEngine engine = new();

    [Fact]
    public void Compare_IdenticalText_ReturnsFullSimilarity()
    {
        var result = engine.Compare("Hallo Welt", "Hallo Welt", CreateOptions());

        Assert.Equal(100d, result.SimilarityPercentage);
        Assert.Equal(0, result.DifferenceCount);
        Assert.Equal(2, result.ComparedUnitCount);
    }

    [Fact]
    public void Compare_DifferentCase_IgnoresCaseByDefault()
    {
        var result = engine.Compare("Hallo", "HALLO", CreateOptions(caseSensitive: false));

        Assert.Equal(100d, result.SimilarityPercentage);
        Assert.Equal(0, result.DifferenceCount);
    }

    [Fact]
    public void Compare_DifferentCase_DetectsDifferenceWhenCaseSensitive()
    {
        var result = engine.Compare("Hallo", "HALLO", CreateOptions(caseSensitive: true));

        Assert.True(result.SimilarityPercentage < 100d);
        Assert.Contains(result.Differences, item => item.Kind != DifferenceKind.Equal);
    }

    [Fact]
    public void Compare_AddedWord_ProducesAddedDifference()
    {
        var result = engine.Compare("eins zwei", "eins zwei drei", CreateOptions());

        Assert.Contains(result.Differences, difference =>
            difference.Kind == DifferenceKind.Added && difference.RightText == "drei");
    }

    [Fact]
    public void Compare_Typo_ProducesChangedDifferenceWithPartialSimilarity()
    {
        var result = engine.Compare("Bestellung", "Bestelung", CreateOptions());

        var difference = Assert.Single(result.Differences);
        Assert.Equal(DifferenceKind.Changed, difference.Kind);
        Assert.InRange(result.SimilarityPercentage, 80d, 99.99d);
    }

    [Fact]
    public void Compare_UnrelatedWords_RemainAddedAndRemoved()
    {
        var result = engine.Compare("rot", "blau", CreateOptions());

        Assert.Contains(result.Differences, item => item.Kind == DifferenceKind.Removed);
        Assert.Contains(result.Differences, item => item.Kind == DifferenceKind.Added);
    }

    [Fact]
    public void Compare_SentenceMode_ComparesCompleteSentences()
    {
        var result = engine.Compare(
            "Erster Satz. Zweiter Satz.",
            "Erster Satz. Zweiter geänderter Satz.",
            CreateOptions(mode: ComparisonMode.Sentences));

        Assert.Equal(2, result.ComparedUnitCount);
        Assert.Contains(result.Differences, item => item.Kind == DifferenceKind.Changed);
    }

    [Fact]
    public void Compare_DisabledNumberComparison_IgnoresDigitChanges()
    {
        var result = engine.Compare(
            "Rechnung 123",
            "Rechnung 987",
            CreateOptions(compareNumbers: false));

        Assert.Equal(100d, result.SimilarityPercentage);
    }

    [Fact]
    public void Compare_DisabledPunctuationComparison_IgnoresPunctuation()
    {
        var result = engine.Compare(
            "Hallo, Welt!",
            "Hallo Welt",
            CreateOptions(comparePunctuation: false));

        Assert.Equal(100d, result.SimilarityPercentage);
    }

    [Fact]
    public void Compare_CancelledToken_ThrowsOperationCancelledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            engine.Compare("eins zwei", "eins drei", CreateOptions(), cancellation.Token));
    }

    [Fact]
    public void Compare_ReportsProgressUntilComplete()
    {
        var reportedValues = new List<int>();
        var progress = new InlineProgress<int>(reportedValues.Add);

        engine.Compare("eins zwei drei", "eins zwei vier", CreateOptions(), progress: progress);

        Assert.NotEmpty(reportedValues);
        Assert.Equal(100, reportedValues[^1]);
    }

    [Fact]
    public void Compare_EmptyDocuments_ReturnFullSimilarity()
    {
        var result = engine.Compare(string.Empty, string.Empty, CreateOptions());

        Assert.Equal(100d, result.SimilarityPercentage);
        Assert.Equal(0, result.DifferenceCount);
        Assert.Equal(0, result.ComparedUnitCount);
    }

    private static ComparisonOptions CreateOptions(
        ComparisonMode mode = ComparisonMode.Words,
        bool caseSensitive = false,
        bool compareNumbers = true,
        bool comparePunctuation = true) =>
        new(
            mode,
            caseSensitive,
            compareNumbers,
            comparePunctuation,
            IgnoreWhitespace: true,
            EnableOcr: false);

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
