using Avalonia.Controls;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class VisitorEditorView : Window
{
    private VisitorEditorViewModel? _viewModel;

    public VisitorEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
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
        base.OnClosing(e);
    }
}
