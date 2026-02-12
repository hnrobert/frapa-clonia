using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.UI.Models;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.ViewModels;

namespace FrapaClonia.UI.Controls;

public partial class ToastNotification : UserControl
{
    private Border? _layoutRoot;
    private Button? _closeButton;
    private TranslateTransform? _transform;
    private bool _isClosing;
    private ToastItem? _toastItem;
    private DispatcherTimer? _animationTimer;

    /// <summary>
    /// Command to close this toast
    /// </summary>
    public ICommand CloseCommand { get; }

    public ToastNotification()
    {
        InitializeComponent();
        CloseCommand = new RelayCommand(CloseToast);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _layoutRoot = this.FindControl<Border>("LayoutRoot");
        _closeButton = this.FindControl<Button>("CloseButton");

        if (_layoutRoot == null) return;

        // Attach click handler directly to close button
        if (_closeButton != null)
        {
            _closeButton.Click += OnCloseButtonClick;
        }

        // Subscribe to close requests from the toast item
        SubscribeToToastItem(DataContext as ToastItem);

        // Create and set the transform for slide animation
        _transform = new TranslateTransform(300, 0);
        _layoutRoot.RenderTransform = _transform;
        _layoutRoot.Opacity = 0;

        // Start slide-in animation
        StartAnimateIn();
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Detach click handler
        if (_closeButton != null)
        {
            _closeButton.Click -= OnCloseButtonClick;
        }

        StopAnimationTimer();
        UnsubscribeFromToastItem();
    }

    private void OnCloseButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CloseToast();
    }

    private void SubscribeToToastItem(ToastItem? toastItem)
    {
        UnsubscribeFromToastItem();
        if (toastItem == null) return;

        _toastItem = toastItem;
        _toastItem.CloseRequested += OnCloseRequested;
    }

    private void UnsubscribeFromToastItem()
    {
        if (_toastItem != null)
        {
            _toastItem.CloseRequested -= OnCloseRequested;
            _toastItem = null;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        CloseToast();
    }

    private void StartAnimateIn()
    {
        StartAnimation(250, 0, 1, 300, 0);
    }

    private void StartAnimateOut()
    {
        StartAnimation(200, 1, 0, 0, 300);
    }

    private void StartAnimation(int durationMs, double startOpacity, double endOpacity, double startX, double endX)
    {
        StopAnimationTimer();

        var totalFrames = durationMs / 16; // ~60fps
        var frameCount = 0;

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _animationTimer.Tick += (_, _) =>
        {
            frameCount++;
            var progress = Math.Min(1.0, (double)frameCount / totalFrames);

            // Apply cubic easing
            var easedProgress = CubicEaseOut(progress);

            if (_layoutRoot != null)
            {
                // Animate opacity
                _layoutRoot.Opacity = startOpacity + (endOpacity - startOpacity) * easedProgress;

                // Animate X position
                if (_transform != null)
                {
                    _transform.X = startX + (endX - startX) * easedProgress;
                }
            }

            if (progress >= 1.0)
            {
                StopAnimationTimer();
                OnAnimationComplete();
            }
        };

        _animationTimer.Start();
    }

    private void StopAnimationTimer()
    {
        _animationTimer?.Stop();
        _animationTimer = null;
    }

    private void OnAnimationComplete()
    {
        // If we just finished animating out, remove the toast
        if (!_isClosing || DataContext is not ToastItem toast) return;
        var service = FindToastService();
        service?.RemoveToast(toast);
    }

    private static double CubicEaseOut(double t)
    {
        return 1 - Math.Pow(1 - t, 3);
    }

    private void CloseToast()
    {
        if (DataContext is not ToastItem || _isClosing) return;
        _isClosing = true;

        // Stop any ongoing animation
        StopAnimationTimer();

        // Start animate out, then remove
        StartAnimateOut();
    }

    private ToastService? FindToastService()
    {
        // Walk up the visual tree to find the MainWindow's DataContext
        var parent = Parent;
        while (parent != null)
        {
            if (parent is TopLevel { DataContext: MainWindowViewModel vm })
            {
                return vm.ToastService;
            }
            parent = parent.Parent;
        }

        // Fallback: try to find via VisualRoot
        return VisualRoot is TopLevel { DataContext: MainWindowViewModel visualVm } ? visualVm.ToastService : null;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not ToastItem toast) return;
        SubscribeToToastItem(toast);
        UpdateVisualState(toast.Level);
    }

    private void UpdateVisualState(ToastLevel level)
    {
        var border = this.FindControl<Border>("LayoutRoot");
        if (border == null) return;

        // Remove all level classes
        border.Classes.Remove("toast-success");
        border.Classes.Remove("toast-info");
        border.Classes.Remove("toast-warning");
        border.Classes.Remove("toast-error");

        // Add the appropriate class (indicator color is handled by CSS selector)
        switch (level)
        {
            case ToastLevel.Success:
                border.Classes.Add("toast-success");
                break;
            case ToastLevel.Info:
                border.Classes.Add("toast-info");
                break;
            case ToastLevel.Warning:
                border.Classes.Add("toast-warning");
                break;
            case ToastLevel.Error:
                border.Classes.Add("toast-error");
                break;
            default:
                border.Classes.Add("toast-info");
                break;
        }
    }
}
