using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.UI.Models;
using FrapaClonia.UI.Services;
using Material.Icons.Avalonia;
using Material.Icons;

namespace FrapaClonia.UI.Controls;

public partial class ToastNotification : UserControl
{
    private ToastService? _toastService;

    /// <summary>
    /// Command to close this toast
    /// </summary>
    public ICommand CloseCommand { get; }

    public ToastNotification()
    {
        InitializeComponent();
        CloseCommand = new RelayCommand(CloseToast);
    }

    /// <summary>
    /// Sets the toast service for this notification
    /// </summary>
    public void SetToastService(ToastService toastService)
    {
        _toastService = toastService;
    }

    private void CloseToast()
    {
        if (DataContext is not ToastItem toast) return;
        // Try to get ToastService from parent ToastContainer
        var service = _toastService ?? FindToastService();
        service?.RemoveToast(toast);
    }

    private ToastService? FindToastService()
    {
        // Walk up the visual tree to find the ToastContainer and get its DataContext
        var parent = Parent;
        while (parent != null)
        {
            if (parent is ToastContainer { DataContext: ToastService service })
            {
                return service;
            }
            parent = parent.Parent;
        }
        return null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ToastItem toast)
        {
            UpdateVisualState(toast.Level);
        }
    }

    private void UpdateVisualState(ToastLevel level)
    {
        var border = this.FindControl<Border>("LayoutRoot");
        var icon = this.FindControl<MaterialIcon>("LevelIcon");

        if (border == null) return;

        // Remove all level classes
        border.Classes.Remove("toast-success");
        border.Classes.Remove("toast-info");
        border.Classes.Remove("toast-warning");
        border.Classes.Remove("toast-error");

        // Add the appropriate class and icon
        switch (level)
        {
            case ToastLevel.Success:
                border.Classes.Add("toast-success");
                icon?.Kind = MaterialIconKind.CheckCircle;
                break;
            case ToastLevel.Info:
                border.Classes.Add("toast-info");
                icon?.Kind = MaterialIconKind.Information;
                break;
            case ToastLevel.Warning:
                border.Classes.Add("toast-warning");
                icon?.Kind = MaterialIconKind.Alert;
                break;
            case ToastLevel.Error:
                border.Classes.Add("toast-error");
                icon?.Kind = MaterialIconKind.AlertCircle;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
