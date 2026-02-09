using FrapaClonia.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Threading;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Provides localizable string bindings for UI elements
/// </summary>
public class LocalizedString : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly string _key;
    private readonly object[] _args;

    public LocalizedString(ILocalizationService localizationService, string key, params object[] args)
    {
        _localizationService = localizationService;
        _key = key;
        _args = args;

        // Subscribe to culture changes and refresh the value
        _localizationService.CultureChanged += OnCultureChanged;

        // Get initial value
        _value = _localizationService.GetString(_key, _args);
    }

    private string _value;

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>
    /// Gets the string value for Avalonia binding
    /// </summary>
    public string StringValue => _value;

    /// <summary>
    /// Implicit conversion to string for convenience
    /// </summary>
    public static implicit operator string(LocalizedString? localizedString)
    {
        return localizedString?._value ?? string.Empty;
    }

    /// <summary>
    /// Override ToString for XAML binding compatibility
    /// </summary>
    public override string ToString()
    {
        return _value;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        // Get the new value synchronously
        var newValue = _localizationService.GetString(_key, _args);

        // Always update on UI thread
        if (Dispatcher.UIThread.CheckAccess())
        {
            UpdateValue(newValue);
        }
        else
        {
            // Post to UI thread - capture newValue to avoid closure issues
            var capturedValue = newValue;
            Dispatcher.UIThread.Post(() => UpdateValue(capturedValue));
        }
    }

    private void UpdateValue(string newValue)
    {
        // Only update if value actually changed
        if (_value == newValue) return;
        _value = newValue;
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(StringValue));
    }
}
