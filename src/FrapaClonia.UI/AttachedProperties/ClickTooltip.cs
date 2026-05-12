using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace FrapaClonia.UI.AttachedProperties;

public static class ClickTooltip
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsEnabled", typeof(ClickTooltip));

    private static readonly Dictionary<TopLevel, HashSet<Button>> TrackedButtons = new();
    private static readonly HashSet<Button> PinnedButtons = [];
    private static readonly Dictionary<Button, (ScrollViewer Sv, EventHandler<ScrollChangedEventArgs> Handler)>
        ScrollSubscriptions = new();
    // Buttons pinned early (on PointerPressed) to prevent ToolTipService from closing the hover tooltip
    // before Click fires. Cleared in OnButtonClick; if Click never fires, cleared in OnButtonPointerReleased.
    private static readonly HashSet<Button> EarlyPinnedButtons = [];

    static ClickTooltip()
    {
        IsEnabledProperty.Changed.AddClassHandler<Button>(OnIsEnabledChanged);
        ToolTip.IsOpenProperty.OverrideMetadata<Button>(new StyledPropertyMetadata<bool>(
            coerce: (obj, value) =>
                !value && obj is Button btn && PinnedButtons.Contains(btn) || value
        ));
    }

    public static void SetIsEnabled(Button obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Button button, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;
        if (enabled)
        {
            button.Click += OnButtonClick;
            button.AddHandler(InputElement.PointerPressedEvent, OnButtonPointerPressed, RoutingStrategies.Tunnel);
            button.AddHandler(InputElement.PointerReleasedEvent, OnButtonPointerReleased, RoutingStrategies.Bubble);
            button.AttachedToVisualTree += OnAttachedToVisualTree;
            button.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }
        else
        {
            button.Click -= OnButtonClick;
            button.RemoveHandler(InputElement.PointerPressedEvent, OnButtonPointerPressed);
            button.RemoveHandler(InputElement.PointerReleasedEvent, OnButtonPointerReleased);
            button.AttachedToVisualTree -= OnAttachedToVisualTree;
            button.DetachedFromVisualTree -= OnDetachedFromVisualTree;
        }
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Button button) return;
        if (TopLevel.GetTopLevel(button) is not { } topLevel) return;
        if (!TrackedButtons.TryGetValue(topLevel, out var buttons))
        {
            buttons = [];
            TrackedButtons[topLevel] = buttons;
            topLevel.AddHandler(InputElement.PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel);
        }
        buttons.Add(button);
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Button button) return;
        Unpin(button);
        EarlyPinnedButtons.Remove(button);
        if (e.Root is not TopLevel topLevel) return;
        if (!TrackedButtons.TryGetValue(topLevel, out var buttons)) return;
        buttons.Remove(button);
        if (buttons.Count != 0) return;
        TrackedButtons.Remove(topLevel);
        topLevel.RemoveHandler(InputElement.PointerPressedEvent, OnTopLevelPointerPressed);
    }

    // Tunnel: fires before ToolTipService's bubble PointerPressed handler.
    // If the hover tooltip is already open, pin immediately so the coerce blocks the close.
    private static void OnButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button) return;
        if (!ToolTip.GetIsOpen(button) || PinnedButtons.Contains(button)) return;
        Pin(button);
        EarlyPinnedButtons.Add(button);
    }

    // Bubble: fires after Click (which fires inside PointerReleased).
    // If Click never fired (e.g. pointer dragged away), clean up the early pin.
    private static void OnButtonPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Button button) return;
        if (!EarlyPinnedButtons.Remove(button)) return;
        Unpin(button);
        if (!button.IsPointerOver)
            ToolTip.SetIsOpen(button, false);
    }

    private static void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var earlyPinned = EarlyPinnedButtons.Remove(button);
        if (PinnedButtons.Contains(button))
        {
            if (earlyPinned) return; // First click: hover tooltip is now pinned — done
            Unpin(button);
            if (!button.IsPointerOver)
                ToolTip.SetIsOpen(button, false);
        }
        else
        {
            Pin(button);
            if (!ToolTip.GetIsOpen(button))
                ToolTip.SetIsOpen(button, true);
        }
    }

    private static void Pin(Button button)
    {
        PinnedButtons.Add(button);
        if (button.FindAncestorOfType<ScrollViewer>() is not { } sv) return;
        void Handler(object? _, ScrollChangedEventArgs __)
        {
            Unpin(button);
            ToolTip.SetIsOpen(button, false);
        }
        sv.ScrollChanged += Handler;
        ScrollSubscriptions[button] = (sv, Handler);
    }

    private static void Unpin(Button button)
    {
        PinnedButtons.Remove(button);
        if (!ScrollSubscriptions.TryGetValue(button, out var sub)) return;
        sub.Sv.ScrollChanged -= sub.Handler;
        ScrollSubscriptions.Remove(button);
    }

    private static void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TopLevel topLevel) return;
        if (!TrackedButtons.TryGetValue(topLevel, out var buttons)) return;
        foreach (var button in buttons.Where(ToolTip.GetIsOpen))
        {
            if (e.Source is Visual source &&
                (ReferenceEquals(source, button) || button.IsVisualAncestorOf(source))) continue;
            Unpin(button);
            ToolTip.SetIsOpen(button, false);
        }
    }
}
