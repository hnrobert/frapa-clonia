using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace FrapaClonia.UI.Utils;

public static class WindowReuse
{
    public static TWindow? ActivateExisting<TWindow>(Func<TWindow, bool>? predicate = null)
        where TWindow : Window
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        var windows = desktop.Windows.OfType<TWindow>();
        if (predicate != null)
        {
            windows = windows.Where(predicate);
        }

        var existing = windows.FirstOrDefault();
        if (existing == null)
        {
            return null;
        }

        if (!existing.IsVisible)
        {
            existing.Show();
        }

        if (existing.WindowState == WindowState.Minimized)
        {
            existing.WindowState = WindowState.Normal;
        }

        // Show() helps with z-order/focus on some platforms (notably macOS).
        existing.Show();
        existing.Activate();
        existing.Focus();
        return existing;
    }
}
