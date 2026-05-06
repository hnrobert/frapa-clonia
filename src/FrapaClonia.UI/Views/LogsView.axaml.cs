using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class LogsView : UserControl
{
    private ScrollViewer? _scrollViewer;
    private double _lastVerticalOffset;
    private bool _programmaticScroll;

    public LogsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel viewModel)
        {
            viewModel.RefreshOnNavigate();
        }

        // Find the internal ScrollViewer inside the TextBox
        _scrollViewer = LogTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
            _lastVerticalOffset = _scrollViewer.Offset.Y;
        }

        LogTextBox.TextChanged += OnLogTextChanged;
    }

    private void OnLogTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is not LogsViewModel vm || !vm.IsFollowEnabled) return;

        _programmaticScroll = true;
        LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
        _scrollViewer?.ScrollToEnd();
        _programmaticScroll = false;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_programmaticScroll || _scrollViewer == null || DataContext is not LogsViewModel vm) return;

        var currentOffset = _scrollViewer.Offset.Y;
        var isScrollingUp = currentOffset < _lastVerticalOffset;
        _lastVerticalOffset = currentOffset;

        var isAtBottom = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - currentOffset < 5;

        if (isAtBottom && !vm.IsFollowEnabled)
        {
            vm.IsFollowEnabled = true;
        }
        else if (isScrollingUp && vm.IsFollowEnabled)
        {
            vm.IsFollowEnabled = false;
        }
    }

    private void OnCopyClicked(object? sender, RoutedEventArgs e) => LogTextBox.Copy();

    private void OnSelectAllClicked(object? sender, RoutedEventArgs e) => LogTextBox.SelectAll();

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel vm)
        {
            vm.ClearLogsCommand.Execute(null);
        }
    }
}
