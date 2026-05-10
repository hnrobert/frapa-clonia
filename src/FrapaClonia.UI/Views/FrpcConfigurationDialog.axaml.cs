using Avalonia.Controls;
using FrapaClonia.UI.Services;
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

        ToastService.Instance?.PushChildWindow();
        Closed += (_, _) => ToastService.Instance?.PopChildWindow();

        var toastContainer = this.FindControl<ItemsControl>("ToastContainer");
        if (toastContainer != null && ToastService.Instance != null)
        {
            toastContainer.ItemsSource = ToastService.Instance.ChildToasts;
        }
    }
}
