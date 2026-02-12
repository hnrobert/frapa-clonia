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
    private bool _isClosing;
    private ToastItem? _toastItem;
    private DispatcherTimer? _animationTimer;
    private double _animationProgress;
    private bool _isAnimatingOut;

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
        if (_layoutRoot == null) return;

        // Subscribe to close requests from the toast item
        SubscribeToToastItem(DataContext as ToastItem);

        // Set initial state: invisible and positioned to the right
        _layoutRoot.Opacity = 0;
        _layoutRoot.RenderTransform = new TranslateTransform(300, 0);

        // Start slide-in animation
        StartAnimateIn();
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimationTimer();
        UnsubscribeFromToastItem();
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
        _isAnimatingOut = false;
        _animationProgress = 0;
        StartAnimation(250, UpdateAnimateIn);
    }

    private void StartAnimateOut()
    {
        _isAnimatingOut = true;
        _animationProgress = 0;
        StartAnimation(200, UpdateAnimateOut);
    }

    private void StartAnimation(int durationMs, Action<double> updateCallback)
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
            _animationProgress = Math.Min(1.0, (double)frameCount / totalFrames);

            // Apply easing (cubic ease out for in, cubic ease in for out)
            var easedProgress = _isAnimatingOut
                ? CubicEaseIn(_animationProgress)
                : CubicEaseOut(_animationProgress);

            updateCallback(easedProgress);

            if (_animationProgress >= 1.0)
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

    private void UpdateAnimateIn(double progress)
    {
        if (_layoutRoot == null) return;

        // Opacity: 0 -> 1
        _layoutRoot.Opacity = progress;

        // X: 300 -> 0
        if (_layoutRoot.RenderTransform is TranslateTransform transform)
        {
            transform.X = 300 * (1 - progress);
        }
    }

    private void UpdateAnimateOut(double progress)
    {
        if (_layoutRoot == null) return;

        // Opacity: 1 -> 0
        _layoutRoot.Opacity = 1 - progress;

        // X: 0 -> 300
        if (_layoutRoot.RenderTransform is TranslateTransform transform)
        {
            transform.X = 300 * progress;
        }
    }

    private void OnAnimationComplete()
    {
        if (_isAnimatingOut)
        {
            // Animation out complete, remove from collection
            if (DataContext is ToastItem toast)
            {
                var service = FindToastService();
                service?.RemoveToast(toast);
            }
        }
    }

    private static double CubicEaseOut(double t)
    {
        return 1 - Math.Pow(1 - t, 3);
    }

    private static double CubicEaseIn(double t)
    {
        return t * t * t;
    }

    // ReSharper disable once AsyncVoidMethod
    private void CloseToast()
    {
        if (DataContext is not ToastItem || _isClosing) return;
        _isClosing = true;

        // Stop any ongoing animation
        StopAnimationTimer();

        // Start animate out
        StartAnimateOut();
    }

    private ToastService? FindToastService()
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is TopLevel { DataContext: MainWindowViewModel { ToastService: not null } vm })
            {
                return vm.ToastService;
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
            SubscribeToToastItem(toast);
            UpdateVisualState(toast.Level);
        }
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
