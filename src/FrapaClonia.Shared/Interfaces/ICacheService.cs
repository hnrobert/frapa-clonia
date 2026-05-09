namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Service for managing application cache data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets the name of the currently selected preset
    /// </summary>
    string? CurrentPresetName { get; }

    /// <summary>
    /// Initializes the cache service, loading cache data from storage
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Sets the current preset by name
    /// </summary>
    Task SetCurrentPresetAsync(string presetName);

    /// <summary>
    /// Saves the cache to storage
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// GitHub personal access token for higher API rate limits
    /// </summary>
    string? GitHubToken { get; set; }
}
