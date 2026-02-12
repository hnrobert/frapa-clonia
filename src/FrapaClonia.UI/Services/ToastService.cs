using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using FrapaClonia.UI.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Service for managing toast notifications across the application
/// </summary>
public class ToastService : ObservableObject
{
    private readonly ILogger<ToastService>? _logger;
    private readonly int _maxToasts = 5;

    /// <summary>
    /// Collection of currently active toasts
    /// </summary>
    public ObservableCollection<ToastItem> Toasts { get; } = [];

    /// <summary>
    /// Event raised when a toast is added
    /// </summary>
    public event EventHandler<ToastItem>? ToastAdded;

    /// <summary>
    /// Event raised when a toast is removed
    /// </summary>
    public event EventHandler<ToastItem>? ToastRemoved;

    public ToastService(ILogger<ToastService>? logger = null)
    {
        _logger = logger;
        Toasts.CollectionChanged += OnToastsCollectionChanged;
    }

    private void OnToastsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // No-op for now, can be used for logging or other side effects
    }

    /// <summary>
    /// Show a success toast notification
    /// </summary>
    public ToastItem Success(string title, string message, int duration = 4000)
    {
        return ShowToast(title, message, ToastLevel.Success, duration);
    }

    /// <summary>
    /// Show an info toast notification
    /// </summary>
    public ToastItem Info(string title, string message, int duration = 4000)
    {
        return ShowToast(title, message, ToastLevel.Info, duration);
    }

    /// <summary>
    /// Show a warning toast notification
    /// </summary>
    public ToastItem Warning(string title, string message, int duration = 6000)
    {
        return ShowToast(title, message, ToastLevel.Warning, duration);
    }

    /// <summary>
    /// Show an error toast notification
    /// </summary>
    public ToastItem Error(string title, string message, int duration = 0)
    {
        return ShowToast(title, message, ToastLevel.Error, duration);
    }

    /// <summary>
    /// Show a toast notification with custom level
    /// </summary>
    public ToastItem ShowToast(string title, string message, ToastLevel level = ToastLevel.Info, int duration = 4000)
    {
        var toast = new ToastItem(title, message, level, duration);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Remove oldest toasts if we exceed max
            while (Toasts.Count >= _maxToasts)
            {
                var oldest = Toasts[0];
                RemoveToast(oldest);
            }

            Toasts.Add(toast);
            ToastAdded?.Invoke(this, toast);
            _logger?.LogDebug("Toast shown: [{Level}] {Title} - {Message}", level, title, message);

            // Set up auto-close if duration > 0
            if (duration > 0)
            {
                _ = AutoCloseAsync(toast, duration);
            }
        });

        return toast;
    }

    /// <summary>
    /// Remove a specific toast
    /// </summary>
    public void RemoveToast(ToastItem toast)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            toast.IsVisible = false;
            Toasts.Remove(toast);
            ToastRemoved?.Invoke(this, toast);
            _logger?.LogDebug("Toast removed: {Title}", toast.Title);
        });
    }

    /// <summary>
    /// Remove a toast by its ID
    /// </summary>
    public void RemoveToast(Guid toastId)
    {
        var toast = Toasts.FirstOrDefault(t => t.Id == toastId);
        if (toast != null)
        {
            RemoveToast(toast);
        }
    }

    /// <summary>
    /// Clear all toasts
    /// </summary>
    public void ClearAll()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            foreach (var toast in Toasts.ToList())
            {
                toast.IsVisible = false;
            }

            Toasts.Clear();
            _logger?.LogDebug("All toasts cleared");
        });
    }

    private async Task AutoCloseAsync(ToastItem toast, int duration)
    {
        await Task.Delay(duration);

        if (Toasts.Contains(toast))
        {
            RemoveToast(toast);
        }
    }
}
