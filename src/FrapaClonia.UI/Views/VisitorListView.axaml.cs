using Avalonia.Controls;
using Avalonia.Interactivity;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class VisitorListView : UserControl
{
    public VisitorListView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: VisitorConfig visitor } ||
            DataContext is not VisitorListViewModel viewModel) return;
        viewModel.SelectedVisitor = visitor;
        if (viewModel.EditVisitorCommand.CanExecute(null))
        {
            viewModel.EditVisitorCommand.Execute(null);
        }
    }

    private void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: VisitorConfig visitor } &&
            DataContext is VisitorListViewModel viewModel)
        {
            viewModel.SelectedVisitor = visitor;
            if (viewModel.DuplicateVisitorCommand.CanExecute(null))
            {
                viewModel.DuplicateVisitorCommand.Execute(null);
            }
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: VisitorConfig visitor } ||
            DataContext is not VisitorListViewModel viewModel) return;
        viewModel.SelectedVisitor = visitor;
        if (viewModel.DeleteVisitorCommand.CanExecute(null))
        {
            viewModel.DeleteVisitorCommand.Execute(null);
        }
    }
}
