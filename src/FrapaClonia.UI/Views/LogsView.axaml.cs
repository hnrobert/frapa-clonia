using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class LogsView : UserControl
{
    private ScrollViewer? _scrollViewer;
    private double _lastVerticalOffset;
    private bool _programmaticScroll;
    private LogsViewModel? _subscribedVm;

    public LogsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnVmPropertyChanged;
        }

        _subscribedVm = DataContext as LogsViewModel;

        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogsViewModel.IsFollowEnabled) && _subscribedVm?.IsFollowEnabled == true)
        {
            // User checked Auto Scroll → scroll to bottom immediately
            _programmaticScroll = true;
            LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
            _scrollViewer?.ScrollToEnd();
            _lastVerticalOffset = _scrollViewer?.Offset.Y ?? 0;
            _programmaticScroll = false;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LogsViewModel viewModel)
        {
            viewModel.RefreshOnNavigate();
            _subscribedVm = viewModel;
            _subscribedVm.PropertyChanged += OnVmPropertyChanged;
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
        if (_subscribedVm is not { IsFollowEnabled: true }) return;

        _programmaticScroll = true;
        LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
        _scrollViewer?.ScrollToEnd();
        _lastVerticalOffset = _scrollViewer?.Offset.Y ?? 0;
        _programmaticScroll = false;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_programmaticScroll || _scrollViewer == null || _subscribedVm == null) return;

        var currentOffset = _scrollViewer.Offset.Y;
        var isScrollingUp = currentOffset < _lastVerticalOffset;
        _lastVerticalOffset = currentOffset;

        var isAtBottom = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height - currentOffset < 5;

        if (isAtBottom && !_subscribedVm.IsFollowEnabled)
        {
            _subscribedVm.IsFollowEnabled = true;
        }
        else if (isScrollingUp && _subscribedVm.IsFollowEnabled)
        {
            _subscribedVm.IsFollowEnabled = false;
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
