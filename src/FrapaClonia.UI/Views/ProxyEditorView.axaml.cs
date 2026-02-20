using Avalonia.Controls;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class ProxyEditorView : Window
{
    private ProxyEditorViewModel? _viewModel;

    public ProxyEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
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
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        // Cancel button clicked - close without saving
        Close(false);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProxyEditorViewModel.IsSaving) && _viewModel is { IsSaving: false, IsValid: true, HasValidationError: false })
        {
            // Save completed successfully, close with true result
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
        base.OnClosing(e);
    }
}
