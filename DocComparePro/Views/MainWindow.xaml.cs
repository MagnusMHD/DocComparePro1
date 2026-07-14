using System.Windows;
using DocComparePro.ViewModels;

namespace DocComparePro.Views;

/// <summary>
/// Contains only view-specific event handling that cannot be expressed through bindings.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the main application window.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LeftDropZone_OnDrop(object sender, DragEventArgs e) => HandleDrop(e, isLeft: true);

    private void RightDropZone_OnDrop(object sender, DragEventArgs e) => HandleDrop(e, isLeft: false);

    private void HandleDrop(DragEventArgs e, bool isLeft)
    {
        if (DataContext is not MainViewModel viewModel ||
            !e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files)
        {
            return;
        }

        // The view forwards only the dropped path; validation remains in the view model/services.
        viewModel.SetDroppedFile(isLeft, files[0]);
    }
}