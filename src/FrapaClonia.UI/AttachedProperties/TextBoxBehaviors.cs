using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace FrapaClonia.UI.AttachedProperties;

/// <summary>
/// Attached properties for TextBox behaviors
/// </summary>
public static class TextBoxBehaviors
{
    /// <summary>
    /// Attached property to enable losing focus on Escape key
    /// </summary>
    public static readonly AttachedProperty<bool> LoseFocusOnEscapeProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("LoseFocusOnEscape", typeof(TextBoxBehaviors));

    static TextBoxBehaviors()
    {
        LoseFocusOnEscapeProperty.Changed.AddClassHandler<TextBox>(OnLoseFocusOnEscapeChanged);
    }

    public static bool GetLoseFocusOnEscape(TextBox obj)
    {
        return obj.GetValue(LoseFocusOnEscapeProperty);
    }

    public static void SetLoseFocusOnEscape(TextBox obj, bool value)
    {
        obj.SetValue(LoseFocusOnEscapeProperty, value);
    }

    private static void OnLoseFocusOnEscapeChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled) return;
        if (enabled)
        {
            textBox.KeyDown += OnTextBoxKeyDown;
        }
        else
        {
            textBox.KeyDown -= OnTextBoxKeyDown;
        }
    }

    private static void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || sender is not TextBox textBox) return;
        // Find a focusable parent to move focus to
        var parent = textBox.GetVisualAncestors()
            .OfType<Control>()
            .FirstOrDefault(c => c.Focusable && c is not TextBox);

        if (parent != null)
        {
            parent.Focus();
        }
        else
        {
            // Fallback: focus the TopLevel (window)
            var topLevel = TopLevel.GetTopLevel(textBox);
            if (topLevel is Control topLevelControl)
            {
                topLevelControl.Focus();
            }
        }

        e.Handled = true;
    }
}