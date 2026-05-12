using System.Collections.ObjectModel;
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
    private int _childWindowCount;

    public static ToastService? Instance { get; private set; }

    /// <summary>
    /// Toasts displayed in the main window
    /// </summary>
    public ObservableCollection<ToastItem> Toasts { get; } = [];

    /// <summary>
    /// Toasts displayed in child windows
    /// </summary>
    public ObservableCollection<ToastItem> ChildToasts { get; } = [];

    public event EventHandler<ToastItem>? ToastAdded;
    public event EventHandler<ToastItem>? ToastRemoved;

    public ToastService(ILogger<ToastService>? logger = null)
    {
        Instance = this;
        _logger = logger;
    }

    /// <summary>
    /// Notify that a child window has opened. Toasts will route to ChildToasts.
    /// </summary>
    public void PushChildWindow()
    {
        _childWindowCount++;
    }

    /// <summary>
    /// Notify that a child window has closed. When no child windows remain,
    /// toasts route back to the main window collection.
    /// </summary>
    public void PopChildWindow()
    {
        _childWindowCount = Math.Max(0, _childWindowCount - 1);
        if (_childWindowCount == 0)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ChildToasts.Clear());
        }
    }

    private bool IsChildWindowActive => _childWindowCount > 0;

    private ObservableCollection<ToastItem> TargetCollection => IsChildWindowActive ? ChildToasts : Toasts;

    public ToastItem Success(string title, string message, int duration = 4000)
    {
        return ShowToast(title, message, ToastLevel.Success, duration);
    }

    public ToastItem Info(string title, string message, int duration = 4000)
    {
        return ShowToast(title, message, ToastLevel.Info, duration);
    }

    public ToastItem Warning(string title, string message, int duration = 6000)
    {
        return ShowToast(title, message, ToastLevel.Warning, duration);
    }

    public ToastItem Error(string title, string message, int duration = 0)
    {
        return ShowToast(title, message, ToastLevel.Error, duration);
    }

    public ToastItem ShowToast(string title, string message, ToastLevel level = ToastLevel.Info, int duration = 4000)
    {
        var toast = new ToastItem(title, message, level, duration);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var target = TargetCollection;

            while (target.Count >= _maxToasts)
            {
                var oldest = target[0];
                RemoveFromCollection(target, oldest);
            }

            target.Add(toast);
            ToastAdded?.Invoke(this, toast);
            _logger?.LogDebug("Toast shown: [{Level}] {Title} - {Message}", level, title, message);

            if (duration > 0)
            {
                _ = AutoCloseAsync(toast, target, duration);
            }
        });

        return toast;
    }

    public void RemoveToast(ToastItem toast)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            toast.IsVisible = false;
            if (!Toasts.Remove(toast) && !ChildToasts.Remove(toast)) return;
            ToastRemoved?.Invoke(this, toast);
            _logger?.LogDebug("Toast removed: {Title}", toast.Title);
        });
    }

    // ReSharper disable once UnusedMember.Global
    public void RemoveToast(Guid toastId)
    {
        if ((Toasts.FirstOrDefault(t => t.Id == toastId) ?? ChildToasts.FirstOrDefault(t => t.Id == toastId)) is { } toast)
            RemoveToast(toast);
    }

    // ReSharper disable once UnusedMember.Global
    public void ClearAll()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            foreach (var toast in Toasts.ToList())
                toast.IsVisible = false;
            foreach (var toast in ChildToasts.ToList())
                toast.IsVisible = false;

            Toasts.Clear();
            ChildToasts.Clear();
            _logger?.LogDebug("All toasts cleared");
        });
    }

    private void RemoveFromCollection(ObservableCollection<ToastItem> collection, ToastItem toast)
    {
        toast.IsVisible = false;
        collection.Remove(toast);
        ToastRemoved?.Invoke(this, toast);
    }

    private static async Task AutoCloseAsync(ToastItem toast, ObservableCollection<ToastItem> collection, int duration)
    {
        await Task.Delay(duration);

        if (collection.Contains(toast))
        {
            toast.RequestClose();
        }
    }
}
