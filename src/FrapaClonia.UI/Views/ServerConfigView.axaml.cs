using Avalonia.Controls;
using Avalonia.Interactivity;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class ServerConfigView : UserControl
{
    public ServerConfigView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ServerConfigViewModel viewModel)
        {
            viewModel.Initialize();
        }
    }
}
