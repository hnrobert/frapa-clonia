using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FrapaClonia.Core.Interfaces;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

namespace FrapaClonia.UI.MarkupExtensions;

/// <summary>
/// A markup extension that provides localized strings that update when the culture changes.
/// Usage in XAML: {local:Localize KeyName}
/// </summary>
public class LocalizeExtension(string key) : MarkupExtension
{
    private static ILocalizationService? _localizationService;
    private static readonly ConcurrentDictionary<string, LocalizeValue> Values = new();
    private static bool _isSubscribed;

    /// <summary>
    /// Gets or sets the localization service (should be set at application startup)
    /// </summary>
    public static ILocalizationService? LocalizationService
    {
        set
        {
            if (_localizationService != null && _isSubscribed)
            {
                _localizationService.CultureChanged -= OnCultureChanged;
                _isSubscribed = false;
            }

            _localizationService = value;

            if (_localizationService == null) return;
            _localizationService.CultureChanged += OnCultureChanged;
            _isSubscribed = true;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Get or create a shared value holder for this key
        var valueHolder = Values.GetOrAdd(key, k => new LocalizeValue(k));

        // Return a binding to the shared value holder
        return new Binding(nameof(LocalizeValue.Value))
        {
            Source = valueHolder,
            Mode = BindingMode.OneWay
        };
    }

    private static void OnCultureChanged(object? sender, EventArgs e)
    {
        // Must update on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var valueHolder in Values.Values)
            {
                valueHolder.Refresh();
            }
        });
    }

    private static string GetString(string key)
    {
        // First try to get from Strings class directly (uses ResourceManager internally)
        // This is more efficient and provides compile-time safety
        var property = typeof(Strings).GetProperty(key);
        if (property != null)
        {
            var value = property.GetValue(null) as string;
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        // Fallback to localization service (for dynamic keys)
        return _localizationService != null ? _localizationService.GetString(key) : key;
    }

    /// <summary>
    /// A shared value holder for each localization key.
    /// Multiple bindings to the same key share the same instance.
    /// </summary>
    private class LocalizeValue(string key) : INotifyPropertyChanged
    {
        public string Value
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        } = GetString(key);

        public void Refresh()
        {
            Value = GetString(key);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
