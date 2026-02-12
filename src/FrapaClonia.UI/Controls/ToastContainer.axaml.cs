using Avalonia;
using Avalonia.Controls;
using FrapaClonia.UI.Services;

namespace FrapaClonia.UI.Controls;

public partial class ToastContainer : UserControl
{
    public ToastContainer()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is ToastService)
        {
        }
    }
}
