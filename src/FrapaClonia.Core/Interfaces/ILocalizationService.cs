using System.Globalization;

namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing application localization and multi-language support
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the list of supported cultures
    /// </summary>
    List<CultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Sets the current culture
    /// </summary>
    void SetCulture(string cultureCode);

    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Event raised when culture changes
    /// </summary>
    event EventHandler? CultureChanged;
}
