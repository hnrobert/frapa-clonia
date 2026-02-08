using FrapaClonia.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

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
        _localizationService.CultureChanged += (_, _) => RefreshValue();

        // Get initial value
        _value = _localizationService.GetString(_key, _args);
    }

    private string _value;

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    private void RefreshValue()
    {
        _value = _localizationService.GetString(_key, _args);
        OnPropertyChanged(nameof(Value));
    }
}
