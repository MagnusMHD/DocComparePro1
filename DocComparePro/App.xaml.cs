using System.Windows;
using DocComparePro.Core;
using DocComparePro.ViewModels;
using DocComparePro.Views;

namespace DocComparePro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IDocumentReader documentReader = new DocumentReader();
        IComparisonEngine comparisonEngine = new ComparisonEngine();
        var viewModel = new MainViewModel(documentReader, comparisonEngine);

        var window = new MainWindow
        {
            DataContext = viewModel
        };

        MainWindow = window;
        window.Show();
    }
}
