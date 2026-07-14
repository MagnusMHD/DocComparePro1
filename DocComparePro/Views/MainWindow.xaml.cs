using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DocComparePro.ViewModels;

namespace DocComparePro.Views;

/// <summary>
/// Contains only view-specific event handling that cannot be expressed through bindings.
/// </summary>
public partial class MainWindow : Window
{
    private ScrollViewer? leftScrollViewer;
    private ScrollViewer? rightScrollViewer;
    private bool isSynchronizingScroll;

    /// <summary>Initializes the main application window.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        leftScrollViewer = FindVisualChild<ScrollViewer>(LeftDocumentList);
        rightScrollViewer = FindVisualChild<ScrollViewer>(RightDocumentList);

        if (leftScrollViewer is not null)
        {
            leftScrollViewer.ScrollChanged += LeftScrollViewer_OnScrollChanged;
        }

        if (rightScrollViewer is not null)
        {
            rightScrollViewer.ScrollChanged += RightScrollViewer_OnScrollChanged;
        }
    }

    private void LeftScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e) =>
        SynchronizeScroll(leftScrollViewer, rightScrollViewer);

    private void RightScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e) =>
        SynchronizeScroll(rightScrollViewer, leftScrollViewer);

    private void SynchronizeScroll(ScrollViewer? source, ScrollViewer? target)
    {
        if (isSynchronizingScroll || source is null || target is null)
        {
            return;
        }

        try
        {
            isSynchronizingScroll = true;
            target.ScrollToVerticalOffset(source.VerticalOffset);
            target.ScrollToHorizontalOffset(source.HorizontalOffset);
        }
        finally
        {
            isSynchronizingScroll = false;
        }
    }

    private async void LeftDropZone_OnDrop(object sender, DragEventArgs e) =>
        await HandleDropAsync(e, isLeft: true);

    private async void RightDropZone_OnDrop(object sender, DragEventArgs e) =>
        await HandleDropAsync(e, isLeft: false);

    private async Task HandleDropAsync(DragEventArgs e, bool isLeft)
    {
        if (DataContext is not MainViewModel viewModel ||
            !e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files)
        {
            return;
        }

        // Loading remains in the view model; the view only forwards the dropped file path.
        await viewModel.SetDroppedFileAsync(isLeft, files[0]);
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
