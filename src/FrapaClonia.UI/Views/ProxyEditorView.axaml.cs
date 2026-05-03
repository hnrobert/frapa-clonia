using Avalonia.Controls;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class ProxyEditorView : Window
{
    private ProxyEditorViewModel? _viewModel;
    private ItemsControl? _toastContainer;

    public ProxyEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ToastService.Instance?.PushChildWindow();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as ProxyEditorViewModel;

        if (_viewModel == null) return;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProxyEditorViewModel.IsSaving) && _viewModel is { IsSaving: false, IsValid: true, HasValidationError: false })
        {
            Close(true);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.CloseRequested -= OnCloseRequested;
        }
        ToastService.Instance?.PopChildWindow();
        base.OnClosing(e);
    }
}
