using Avalonia.Controls;
using Avalonia.Interactivity;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class ProxyListView : UserControl
{
    public ProxyListView()
    {
        InitializeComponent();
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ProxyConfig proxy } ||
            DataContext is not ProxyListViewModel viewModel) return;
        viewModel.SelectedProxy = proxy;
        if (viewModel.EditProxyCommand.CanExecute(null))
        {
            viewModel.EditProxyCommand.Execute(null);
        }
    }

    private void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ProxyConfig proxy } &&
            DataContext is ProxyListViewModel viewModel)
        {
            viewModel.SelectedProxy = proxy;
            if (viewModel.DuplicateProxyCommand.CanExecute(null))
            {
                viewModel.DuplicateProxyCommand.Execute(null);
            }
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ProxyConfig proxy } ||
            DataContext is not ProxyListViewModel viewModel) return;
        viewModel.SelectedProxy = proxy;
        if (viewModel.DeleteProxyCommand.CanExecute(null))
        {
            viewModel.DeleteProxyCommand.Execute(null);
        }
    }
}
