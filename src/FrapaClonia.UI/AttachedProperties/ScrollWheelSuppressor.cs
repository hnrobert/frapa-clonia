using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace FrapaClonia.UI.AttachedProperties;

public static class ScrollWheelSuppressor
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ScrollWheelSuppressor));

    static ScrollWheelSuppressor()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    public static void SetIsEnabled(Control obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;

        if (enabled)
            control.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        else
            control.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
    }

    private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Let open ComboBox dropdown scroll its own list
        if (sender is ComboBox { IsDropDownOpen: true }) return;
        if (sender is not (ComboBox or TextBox)) return;

        // TextBox: allow pure horizontal scroll within the control;
        // if vertical component exists it's a page-scroll gesture, pass everything to parent
        if (sender is TextBox && e.Delta.X != 0 && e.Delta.Y == 0) return;

        e.Handled = true;

        if ((sender as Control)?.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer) return;

        scrollViewer.Offset = new Vector(
            scrollViewer.Offset.X - e.Delta.X * 50,
            scrollViewer.Offset.Y - e.Delta.Y * 50);
    }
}
