using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocComparePro.Core;
using Microsoft.Win32;

namespace DocComparePro.ViewModels;

/// <summary>
/// Coordinates file selection, automatic preview, comparison, result display and report export.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff" };

    private readonly IDocumentReader documentReader;
    private readonly IComparisonEngine comparisonEngine;
    private readonly IReportExporter reportExporter;
    private readonly IFileLogger logger;
    private ComparisonResult? currentResult;
    private DocumentContent? leftDocument;
    private DocumentContent? rightDocument;
    private CancellationTokenSource? operationCancellation;

    [ObservableProperty] private string leftFilePath = string.Empty;
    [ObservableProperty] private string rightFilePath = string.Empty;
    [ObservableProperty] private string leftFileName = "Keine Datei ausgewählt";
    [ObservableProperty] private string rightFileName = "Keine Datei ausgewählt";
    [ObservableProperty] private string? leftImagePath;
    [ObservableProperty] private string? rightImagePath;
    [ObservableProperty] private bool isLeftImage;
    [ObservableProperty] private bool isRightImage;
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

    /// <summary>Contains the visible lines or aligned comparison units for document A.</summary>
    public ObservableCollection<DifferenceItem> LeftPreviewItems { get; } = new();

    /// <summary>Contains the visible lines or aligned comparison units for document B.</summary>
    public ObservableCollection<DifferenceItem> RightPreviewItems { get; } = new();

    /// <summary>Contains only added, removed and changed units for the compact result table.</summary>
    public ObservableCollection<DifferenceItem> Differences { get; } = new();

    /// <summary>Creates the main view model with all required application services.</summary>
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
    private async Task SelectLeftFileAsync() =>
        await SetFileAsync(isLeft: true, ShowFileDialog());

    [RelayCommand]
    private async Task SelectRightFileAsync() =>
        await SetFileAsync(isLeft: false, ShowFileDialog());

    /// <summary>Accepts a path from the view's drag-and-drop event.</summary>
    public Task SetDroppedFileAsync(bool isLeft, string? filePath) => SetFileAsync(isLeft, filePath);

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task CompareAsync() => await LoadAndCompareAsync(reloadDocuments: true);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelComparison()
    {
        StatusMessage = "Vorgang wird abgebrochen …";
        operationCancellation?.Cancel();
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
            await reportExporter.ExportAsync(dialog.FileName, LeftFileName, RightFileName, currentResult);
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
        operationCancellation?.Cancel();
        LeftFilePath = string.Empty;
        RightFilePath = string.Empty;
        LeftFileName = "Keine Datei ausgewählt";
        RightFileName = "Keine Datei ausgewählt";
        LeftImagePath = null;
        RightImagePath = null;
        IsLeftImage = false;
        IsRightImage = false;
        leftDocument = null;
        rightDocument = null;
        ResetResult(clearPreviews: true);
        StatusMessage = "Bereit";
        NotifyCommandStates();
    }

    private async Task SetFileAsync(bool isLeft, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        operationCancellation?.Cancel();
        operationCancellation?.Dispose();
        operationCancellation = new CancellationTokenSource();
        var cancellationToken = operationCancellation.Token;

        IsBusy = true;
        ComparisonProgress = 0;

        try
        {
            if (isLeft)
            {
                LeftFilePath = path;
                LeftFileName = Path.GetFileName(path);
                IsLeftImage = IsImage(path);
                LeftImagePath = IsLeftImage ? path : null;
                StatusMessage = "Vorschau für Dokument A wird geladen …";
                leftDocument = await documentReader.ReadAsync(path, EnableOcr, cancellationToken);
                ShowSingleDocumentPreview(leftDocument, isLeft: true);
            }
            else
            {
                RightFilePath = path;
                RightFileName = Path.GetFileName(path);
                IsRightImage = IsImage(path);
                RightImagePath = IsRightImage ? path : null;
                StatusMessage = "Vorschau für Dokument B wird geladen …";
                rightDocument = await documentReader.ReadAsync(path, EnableOcr, cancellationToken);
                ShowSingleDocumentPreview(rightDocument, isLeft: false);
            }

            ResetStatisticsOnly();
            StatusMessage = "Dokumentvorschau geladen.";

            // As soon as both documents are available, compare them without another button click.
            if (leftDocument is not null && rightDocument is not null)
            {
                await CompareLoadedDocumentsAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Laden wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            await logger.LogErrorAsync("Dokumentvorschau konnte nicht geladen werden.", exception);
            StatusMessage = $"Fehler: {exception.Message}";
            ClearFailedSide(isLeft);
        }
        finally
        {
            IsBusy = false;
            operationCancellation?.Dispose();
            operationCancellation = null;
            NotifyCommandStates();
        }
    }

    private async Task LoadAndCompareAsync(bool reloadDocuments)
    {
        operationCancellation?.Cancel();
        operationCancellation?.Dispose();
        operationCancellation = new CancellationTokenSource();
        var cancellationToken = operationCancellation.Token;

        IsBusy = true;
        ComparisonProgress = 0;

        try
        {
            if (reloadDocuments || leftDocument is null || rightDocument is null)
            {
                StatusMessage = "Dokumente werden gelesen …";
                var leftTask = documentReader.ReadAsync(LeftFilePath, EnableOcr, cancellationToken);
                var rightTask = documentReader.ReadAsync(RightFilePath, EnableOcr, cancellationToken);
                await Task.WhenAll(leftTask, rightTask);
                leftDocument = leftTask.Result;
                rightDocument = rightTask.Result;
            }

            await CompareLoadedDocumentsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Vergleich wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            ResetResult(clearPreviews: false);
            await logger.LogErrorAsync("Dokumentvergleich fehlgeschlagen.", exception);
            StatusMessage = $"Fehler: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            operationCancellation?.Dispose();
            operationCancellation = null;
            NotifyCommandStates();
        }
    }

    private async Task CompareLoadedDocumentsAsync(CancellationToken cancellationToken)
    {
        if (leftDocument is null || rightDocument is null)
        {
            return;
        }

        StatusMessage = "Dokumente werden automatisch verglichen …";
        var progress = new Progress<int>(value => ComparisonProgress = value);
        var options = CreateOptions();

        currentResult = await Task.Run(() =>
            comparisonEngine.Compare(
                leftDocument.Text,
                rightDocument.Text,
                options,
                cancellationToken,
                progress), cancellationToken);

        ApplyResult(currentResult);
        StatusMessage = "Vorschau und markierte Unterschiede sind aktuell.";
    }

    private void ShowSingleDocumentPreview(DocumentContent document, bool isLeft)
    {
        var target = isLeft ? LeftPreviewItems : RightPreviewItems;
        target.Clear();

        var lines = document.Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
        {
            lines = new[] { "Kein darstellbarer Text gefunden." };
        }

        for (var index = 0; index < lines.Length; index++)
        {
            target.Add(isLeft
                ? new DifferenceItem(DifferenceKind.Equal, lines[index], string.Empty, index + 1)
                : new DifferenceItem(DifferenceKind.Equal, string.Empty, lines[index], index + 1));
        }
    }

    private void ApplyResult(ComparisonResult result)
    {
        LeftPreviewItems.Clear();
        RightPreviewItems.Clear();
        Differences.Clear();

        foreach (var difference in result.Differences)
        {
            LeftPreviewItems.Add(difference);
            RightPreviewItems.Add(difference);
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

    private void ClearFailedSide(bool isLeft)
    {
        if (isLeft)
        {
            leftDocument = null;
            LeftPreviewItems.Clear();
        }
        else
        {
            rightDocument = null;
            RightPreviewItems.Clear();
        }
    }

    private void ResetResult(bool clearPreviews)
    {
        currentResult = null;
        if (clearPreviews)
        {
            LeftPreviewItems.Clear();
            RightPreviewItems.Clear();
        }

        Differences.Clear();
        SelectedDifference = null;
        ResetStatisticsOnly();
    }

    private void ResetStatisticsOnly()
    {
        currentResult = null;
        Differences.Clear();
        SelectedDifference = null;
        SimilarityPercentage = 0;
        DifferenceCount = 0;
        ComparedUnitCount = 0;
        ProcessingTime = "0 ms";
        ComparisonProgress = 0;
        ExportCommand.NotifyCanExecuteChanged();
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

    private static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path));

    private static string? ShowFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Dokument auswählen",
            Filter = "Unterstützte Dateien|*.txt;*.pdf;*.docx;*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Textdateien|*.txt|PDF-Dokumente|*.pdf|Word-Dokumente|*.docx|Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
