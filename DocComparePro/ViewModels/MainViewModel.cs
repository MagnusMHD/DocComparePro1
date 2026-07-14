using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocComparePro.Core;
using Microsoft.Win32;

namespace DocComparePro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDocumentReader documentReader;
    private readonly IComparisonEngine comparisonEngine;

    [ObservableProperty]
    private string leftFilePath = string.Empty;

    [ObservableProperty]
    private string rightFilePath = string.Empty;

    [ObservableProperty]
    private string leftFileName = "Keine Datei ausgewählt";

    [ObservableProperty]
    private string rightFileName = "Keine Datei ausgewählt";

    [ObservableProperty]
    private string leftPreview = string.Empty;

    [ObservableProperty]
    private string rightPreview = string.Empty;

    [ObservableProperty]
    private bool caseSensitive;

    [ObservableProperty]
    private bool compareNumbers = true;

    [ObservableProperty]
    private bool comparePunctuation = true;

    [ObservableProperty]
    private bool ignoreWhitespace = true;

    [ObservableProperty]
    private bool enableOcr = true;

    [ObservableProperty]
    private bool useSentenceMode;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private double similarityPercentage;

    [ObservableProperty]
    private int differenceCount;

    [ObservableProperty]
    private int comparedUnitCount;

    [ObservableProperty]
    private string processingTime = "0 ms";

    [ObservableProperty]
    private string statusMessage = "Bereit";

    public ObservableCollection<DifferenceItem> Differences { get; } = new();

    public MainViewModel(IDocumentReader documentReader, IComparisonEngine comparisonEngine)
    {
        this.documentReader = documentReader;
        this.comparisonEngine = comparisonEngine;
    }

    [RelayCommand]
    private void SelectLeftFile()
    {
        var path = ShowFileDialog();
        if (path is null)
        {
            return;
        }

        LeftFilePath = path;
        LeftFileName = Path.GetFileName(path);
        ResetResult();
        CompareCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectRightFile()
    {
        var path = ShowFileDialog();
        if (path is null)
        {
            return;
        }

        RightFilePath = path;
        RightFileName = Path.GetFileName(path);
        ResetResult();
        CompareCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync()
    {
        IsBusy = true;
        StatusMessage = "Dokumente werden gelesen und verglichen …";

        try
        {
            var options = CreateOptions();
            var leftTask = documentReader.ReadAsync(LeftFilePath, options.EnableOcr);
            var rightTask = documentReader.ReadAsync(RightFilePath, options.EnableOcr);
            await Task.WhenAll(leftTask, rightTask);

            var result = await Task.Run(() =>
                comparisonEngine.Compare(leftTask.Result.Text, rightTask.Result.Text, options));

            ApplyResult(result);
            StatusMessage = "Vergleich erfolgreich abgeschlossen.";
        }
        catch (Exception exception)
        {
            ResetResult();
            StatusMessage = $"Fehler: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        LeftFilePath = string.Empty;
        RightFilePath = string.Empty;
        LeftFileName = "Keine Datei ausgewählt";
        RightFileName = "Keine Datei ausgewählt";
        ResetResult();
        StatusMessage = "Bereit";
        CompareCommand.NotifyCanExecuteChanged();
    }

    private bool CanCompare() =>
        !IsBusy && File.Exists(LeftFilePath) && File.Exists(RightFilePath);

    partial void OnIsBusyChanged(bool value) => CompareCommand.NotifyCanExecuteChanged();

    private ComparisonOptions CreateOptions() => new(
        UseSentenceMode ? ComparisonMode.Sentences : ComparisonMode.Words,
        CaseSensitive,
        CompareNumbers,
        ComparePunctuation,
        IgnoreWhitespace,
        EnableOcr);

    private void ApplyResult(ComparisonResult result)
    {
        Differences.Clear();
        foreach (var difference in result.Differences.Where(item => item.Kind != DifferenceKind.Equal))
        {
            Differences.Add(difference);
        }

        LeftPreview = result.LeftPreview;
        RightPreview = result.RightPreview;
        SimilarityPercentage = result.SimilarityPercentage;
        DifferenceCount = result.DifferenceCount;
        ComparedUnitCount = result.ComparedUnitCount;
        ProcessingTime = $"{result.ProcessingTime.TotalMilliseconds:N0} ms";
    }

    private void ResetResult()
    {
        Differences.Clear();
        LeftPreview = string.Empty;
        RightPreview = string.Empty;
        SimilarityPercentage = 0;
        DifferenceCount = 0;
        ComparedUnitCount = 0;
        ProcessingTime = "0 ms";
    }

    private static string? ShowFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Dokument auswählen",
            Filter = "Unterstützte Dateien|*.txt;*.pdf;*.png;*.jpg;*.jpeg|Textdateien|*.txt|PDF-Dokumente|*.pdf|Bilder|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
