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

    static ClickTooltip()
    {
        IsEnabledProperty.Changed.AddClassHandler<Button>(OnIsEnabledChanged);
    }

    public static void SetIsEnabled(Button obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Button button, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;

        if (enabled)
        {
            button.Click += OnButtonClick;
            button.AttachedToVisualTree += OnAttachedToVisualTree;
            button.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }
        else
        {
            button.Click -= OnButtonClick;
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
        if (e.Root is not TopLevel topLevel) return;

        if (!TrackedButtons.TryGetValue(topLevel, out var buttons)) return;

        buttons.Remove(button);
        if (buttons.Count != 0) return;

        TrackedButtons.Remove(topLevel);
        topLevel.RemoveHandler(InputElement.PointerPressedEvent, OnTopLevelPointerPressed);
    }

    private static void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        ToolTip.SetIsOpen(button, !ToolTip.GetIsOpen(button));
    }

    private static void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TopLevel topLevel) return;
        if (!TrackedButtons.TryGetValue(topLevel, out var buttons)) return;

        foreach (var button in buttons.Where(ToolTip.GetIsOpen))
        {
            if (e.Source is Visual source &&
                (ReferenceEquals(source, button) || button.IsVisualAncestorOf(source))) continue;
            ToolTip.SetIsOpen(button, false);
        }
    }
}
