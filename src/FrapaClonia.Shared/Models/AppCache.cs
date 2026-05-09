namespace FrapaClonia.Shared.Models;

/// <summary>
/// Application cache data stored in cache.toml
/// </summary>
public class AppCache
{
    /// <summary>
    /// Current preset information
    /// </summary>
    public CurrentPresetInfo CurrentPreset { get; set; } = new();

    /// <summary>
    /// Application information
    /// </summary>
    public AppInfo App { get; set; } = new();
}

/// <summary>
/// Information about the currently selected preset
/// </summary>
public class CurrentPresetInfo
{
    /// <summary>
    /// ID of the current preset
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Application-level information
/// </summary>
public class AppInfo
{
    /// <summary>
    /// Last update check timestamp
    /// </summary>
    public DateTime? LastCheck { get; set; }

    /// <summary>
    /// GitHub personal access token for higher API rate limits
    /// </summary>
    public string? GitHubToken { get; set; }
}
