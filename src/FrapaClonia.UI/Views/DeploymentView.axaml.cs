using Avalonia.Controls;
using Avalonia.Interactivity;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class DeploymentView : UserControl
{
    public DeploymentView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DeploymentViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }
}
