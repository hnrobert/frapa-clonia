using Avalonia.Controls;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.Views;

public partial class AboutView : Window
{
    public AboutView()
    {
        InitializeComponent();
    }

    public AboutView(AboutViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
