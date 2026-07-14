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
        Assert.Contains(result.Differences, item => item.Kind == DifferenceKind.Changed);
    }

    [Fact]
    public void Compare_AddedWord_ProducesAddedDifference()
    {
        var result = engine.Compare("eins zwei", "eins zwei drei", CreateOptions());

        Assert.Contains(result.Differences, difference =>
            difference.Kind == DifferenceKind.Added && difference.RightText == "drei");
    }

    [Fact]
    public void Compare_ReplacedWord_ProducesChangedDifference()
    {
        var result = engine.Compare("rot", "blau", CreateOptions());

        var difference = Assert.Single(result.Differences);
        Assert.Equal(DifferenceKind.Changed, difference.Kind);
        Assert.Equal("rot", difference.LeftText);
        Assert.Equal("blau", difference.RightText);
    }

    [Fact]
    public void Compare_SentenceMode_ComparesCompleteSentences()
    {
        var result = engine.Compare(
            "Erster Satz. Zweiter Satz.",
            "Erster Satz. Geänderter Satz.",
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
}