using Avalonia.Controls;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Views;

public partial class FrpcConfigurationDialog : Window
{
    public FrpcConfigurationDialog()
    {
        InitializeComponent();
    }

    public FrpcConfigurationDialog(FrpcConfigurationViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close(viewModel.DialogResult);
    }
}
