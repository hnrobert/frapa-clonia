using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SubscribeToViewModel();
        RefreshViewModel();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        SubscribeToViewModel();
        RefreshViewModel();
    }

    private void RefreshViewModel()
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            viewModel.RefreshPresetInfo();
        }
    }

    private void SubscribeToViewModel()
    {
        if (DataContext is not DashboardViewModel viewModel) return;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DashboardViewModel.IsRenaming) && viewModel.IsRenaming)
            {
                SelectAllInTextBox();
            }
        };
    }

    private void OnPresetNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not DashboardViewModel viewModel) return;

        if (!viewModel.IsRenaming || !viewModel.RenamePresetCommand.CanExecute(null)) return;
        viewModel.RenamePresetCommand.Execute(null);
        e.Handled = true;
    }

    private void SelectAllInTextBox()
    {
        var textBox = this.FindDescendantOfType<TextBox>();
        if (textBox == null) return;

        textBox.Focus();
        textBox.SelectAll();
    }
}