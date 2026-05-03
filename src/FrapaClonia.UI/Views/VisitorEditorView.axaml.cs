using Avalonia.Controls;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class VisitorEditorView : Window
{
    private VisitorEditorViewModel? _viewModel;
    private ItemsControl? _toastContainer;

    public VisitorEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ToastService.Instance?.PushChildWindow();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as VisitorEditorViewModel;

        if (_viewModel == null) return;
        _viewModel.CloseRequested += OnCloseRequested;

        _toastContainer ??= this.FindControl<ItemsControl>("ToastContainer");
        if (_toastContainer != null && ToastService.Instance != null)
        {
            _toastContainer.ItemsSource = ToastService.Instance.ChildToasts;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close(false);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }
        ToastService.Instance?.PopChildWindow();
        base.OnClosing(e);
    }
}
