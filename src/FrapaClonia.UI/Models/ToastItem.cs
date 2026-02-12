using CommunityToolkit.Mvvm.ComponentModel;

namespace FrapaClonia.UI.Models;

/// <summary>
/// Represents the severity level of a toast notification
/// </summary>
public enum ToastLevel
{
    /// <summary>
    /// Success message (green accent)
    /// </summary>
    Success,

    /// <summary>
    /// Informational message (blue accent)
    /// </summary>
    Info,

    /// <summary>
    /// Warning message (yellow/orange accent)
    /// </summary>
    Warning,

    /// <summary>
    /// Error message (red accent)
    /// </summary>
    Error
}

/// <summary>
/// Represents a single toast notification
/// </summary>
public partial class ToastItem : ObservableObject
{
    /// <summary>
    /// Unique identifier for this toast
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The title of the toast notification
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// The main message content
    /// </summary>
    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>
    /// The severity level of the notification
    /// </summary>
    [ObservableProperty]
    private ToastLevel _level = ToastLevel.Info;

    /// <summary>
    /// Auto-close duration in milliseconds. 0 = no auto-close.
    /// </summary>
    [ObservableProperty]
    private int _duration = 4000;

    /// <summary>
    /// Whether the toast is currently visible (for animation purposes)
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Timestamp when the toast was created
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// Creates a new toast item
    /// </summary>
    public ToastItem() { }

    /// <summary>
    /// Creates a new toast item with specified values
    /// </summary>
    public ToastItem(string title, string message, ToastLevel level = ToastLevel.Info, int duration = 4000)
    {
        Title = title;
        Message = message;
        Level = level;
        Duration = duration;
    }
}
