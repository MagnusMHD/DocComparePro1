using DocComparePro.Core;

namespace DocComparePro.Tests;

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
        Assert.True(result.DifferenceCount > 0);
    }

    [Fact]
    public void Compare_AddedWord_ProducesAddedDifference()
    {
        var result = engine.Compare("eins zwei", "eins zwei drei", CreateOptions());

        Assert.Contains(result.Differences, difference =>
            difference.Kind == DifferenceKind.Added && difference.RightText == "drei");
    }

    [Fact]
    public void Compare_SentenceMode_ComparesCompleteSentences()
    {
        var result = engine.Compare(
            "Erster Satz. Zweiter Satz.",
            "Erster Satz. Geänderter Satz.",
            CreateOptions(mode: ComparisonMode.Sentences));

        Assert.Equal(2, result.ComparedUnitCount);
        Assert.True(result.DifferenceCount > 0);
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
        bool caseSensitive = false) =>
        new(
            mode,
            caseSensitive,
            CompareNumbers: true,
            ComparePunctuation: true,
            IgnoreWhitespace: true,
            EnableOcr: false);
}
