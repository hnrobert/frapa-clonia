using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

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
        if (sender is ComboBox or TextBox)
            e.Handled = true;
    }
}
