using FrapaClonia.Shared.Models;

namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Service for managing application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from storage
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves settings to storage
    /// </summary>
    Task SaveAsync();
}
