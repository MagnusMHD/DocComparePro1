using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocComparePro.Core;
using Microsoft.Win32;

namespace DocComparePro.ViewModels;

/// <summary>
/// Coordinates file selection, comparison, result display and report export.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDocumentReader documentReader;
    private readonly IComparisonEngine comparisonEngine;
    private readonly IReportExporter reportExporter;
    private readonly IFileLogger logger;
    private ComparisonResult? currentResult;
    private CancellationTokenSource? comparisonCancellation;

    [ObservableProperty] private string leftFilePath = string.Empty;
    [ObservableProperty] private string rightFilePath = string.Empty;
    [ObservableProperty] private string leftFileName = "Keine Datei ausgewählt";
    [ObservableProperty] private string rightFileName = "Keine Datei ausgewählt";
    [ObservableProperty] private bool caseSensitive;
    [ObservableProperty] private bool compareNumbers = true;
    [ObservableProperty] private bool comparePunctuation = true;
    [ObservableProperty] private bool ignoreWhitespace = true;
    [ObservableProperty] private bool enableOcr = true;
    [ObservableProperty] private bool useSentenceMode;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int comparisonProgress;
    [ObservableProperty] private double similarityPercentage;
    [ObservableProperty] private int differenceCount;
    [ObservableProperty] private int comparedUnitCount;
    [ObservableProperty] private string processingTime = "0 ms";
    [ObservableProperty] private string statusMessage = "Bereit";
    [ObservableProperty] private DifferenceItem? selectedDifference;

    /// <summary>Contains every aligned unit for the synchronized document views.</summary>
    public ObservableCollection<DifferenceItem> AlignedDifferences { get; } = new();

    /// <summary>Contains only added, removed and changed units for the compact result table.</summary>
    public ObservableCollection<DifferenceItem> Differences { get; } = new();

    /// <summary>
    /// Creates the main view model with all required application services.
    /// </summary>
    public MainViewModel(
        IDocumentReader documentReader,
        IComparisonEngine comparisonEngine,
        IReportExporter reportExporter,
        IFileLogger logger)
    {
        this.documentReader = documentReader;
        this.comparisonEngine = comparisonEngine;
        this.reportExporter = reportExporter;
        this.logger = logger;
    }

    [RelayCommand]
    private void SelectLeftFile() => SetFile(isLeft: true, ShowFileDialog());

    [RelayCommand]
    private void SelectRightFile() => SetFile(isLeft: false, ShowFileDialog());

    /// <summary>
    /// Accepts a path from the view's drag-and-drop event.
    /// </summary>
    public void SetDroppedFile(bool isLeft, string? filePath) => SetFile(isLeft, filePath);

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync()
    {
        comparisonCancellation?.Dispose();
        comparisonCancellation = new CancellationTokenSource();
        var cancellationToken = comparisonCancellation.Token;

        IsBusy = true;
        ComparisonProgress = 0;
        StatusMessage = "Dokumente werden gelesen …";

        try
        {
            var options = CreateOptions();
            var leftTask = documentReader.ReadAsync(LeftFilePath, options.EnableOcr, cancellationToken);
            var rightTask = documentReader.ReadAsync(RightFilePath, options.EnableOcr, cancellationToken);
            await Task.WhenAll(leftTask, rightTask);

            cancellationToken.ThrowIfCancellationRequested();
            StatusMessage = "Dokumente werden verglichen …";
            var progress = new Progress<int>(value => ComparisonProgress = value);

            currentResult = await Task.Run(() =>
                comparisonEngine.Compare(
                    leftTask.Result.Text,
                    rightTask.Result.Text,
                    options,
                    cancellationToken,
                    progress),
                cancellationToken);

            ApplyResult(currentResult);
            StatusMessage = "Vergleich erfolgreich abgeschlossen.";
        }
        catch (OperationCanceledException)
        {
            ResetResult();
            StatusMessage = "Vergleich wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            ResetResult();
            await logger.LogErrorAsync("Dokumentvergleich fehlgeschlagen.", exception);
            StatusMessage = $"Fehler: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            comparisonCancellation?.Dispose();
            comparisonCancellation = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelComparison()
    {
        StatusMessage = "Vergleich wird abgebrochen …";
        comparisonCancellation?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        if (currentResult is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Vergleichsbericht speichern",
            FileName = $"DocCompare_{DateTime.Now:yyyyMMdd_HHmmss}",
            Filter = "HTML-Bericht|*.html|CSV-Datei|*.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await reportExporter.ExportAsync(
                dialog.FileName,
                LeftFileName,
                RightFileName,
                currentResult);
            StatusMessage = $"Bericht gespeichert: {dialog.FileName}";
        }
        catch (Exception exception)
        {
            await logger.LogErrorAsync("Berichtsexport fehlgeschlagen.", exception);
            StatusMessage = $"Exportfehler: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        comparisonCancellation?.Cancel();
        LeftFilePath = string.Empty;
        RightFilePath = string.Empty;
        LeftFileName = "Keine Datei ausgewählt";
        RightFileName = "Keine Datei ausgewählt";
        ResetResult();
        StatusMessage = "Bereit";
        NotifyCommandStates();
    }

    private void SetFile(bool isLeft, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (isLeft)
        {
            LeftFilePath = path;
            LeftFileName = Path.GetFileName(path);
        }
        else
        {
            RightFilePath = path;
            RightFileName = Path.GetFileName(path);
        }

        ResetResult();
        NotifyCommandStates();
    }

    private bool CanCompare() =>
        !IsBusy && File.Exists(LeftFilePath) && File.Exists(RightFilePath);

    private bool CanCancel() => IsBusy;

    private bool CanExport() => !IsBusy && currentResult is not null;

    partial void OnIsBusyChanged(bool value) => NotifyCommandStates();

    private void NotifyCommandStates()
    {
        CompareCommand.NotifyCanExecuteChanged();
        CancelComparisonCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
    }

    private ComparisonOptions CreateOptions() => new(
        UseSentenceMode ? ComparisonMode.Sentences : ComparisonMode.Words,
        CaseSensitive,
        CompareNumbers,
        ComparePunctuation,
        IgnoreWhitespace,
        EnableOcr);

    private void ApplyResult(ComparisonResult result)
    {
        AlignedDifferences.Clear();
        Differences.Clear();

        foreach (var difference in result.Differences)
        {
            AlignedDifferences.Add(difference);
            if (difference.Kind != DifferenceKind.Equal)
            {
                Differences.Add(difference);
            }
        }

        SimilarityPercentage = result.SimilarityPercentage;
        DifferenceCount = result.DifferenceCount;
        ComparedUnitCount = result.ComparedUnitCount;
        ProcessingTime = $"{result.ProcessingTime.TotalMilliseconds:N0} ms";
        SelectedDifference = Differences.FirstOrDefault();
        ComparisonProgress = 100;
        NotifyCommandStates();
    }

    private void ResetResult()
    {
        currentResult = null;
        AlignedDifferences.Clear();
        Differences.Clear();
        SelectedDifference = null;
        SimilarityPercentage = 0;
        DifferenceCount = 0;
        ComparedUnitCount = 0;
        ProcessingTime = "0 ms";
        ComparisonProgress = 0;
        ExportCommand.NotifyCanExecuteChanged();
    }

    private static string? ShowFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Dokument auswählen",
            Filter = "Unterstützte Dateien|*.txt;*.pdf;*.docx;*.png;*.jpg;*.jpeg|Textdateien|*.txt|PDF-Dokumente|*.pdf|Word-Dokumente|*.docx|Bilder|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
