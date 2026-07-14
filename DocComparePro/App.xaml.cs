using System.Windows;
using DocComparePro.Core;
using DocComparePro.ViewModels;
using DocComparePro.Views;

namespace DocComparePro;

/// <summary>
/// Configures application services and opens the main window.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IDocumentReader documentReader = new DocumentReader();
        IComparisonEngine comparisonEngine = new ComparisonEngine();
        IReportExporter reportExporter = new ReportExporter();
        IFileLogger logger = new FileLogger();

        var viewModel = new MainViewModel(
            documentReader,
            comparisonEngine,
            reportExporter,
            logger);

        var window = new MainWindow
        {
            DataContext = viewModel
        };

        MainWindow = window;
        window.Show();
    }
}