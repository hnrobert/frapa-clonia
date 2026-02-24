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
    /// Name of the current preset
    /// </summary>
    public string Name { get; set; } = "";
}

/// <summary>
/// Application-level information
/// </summary>
public class AppInfo
{
    /// <summary>
    /// Current language setting
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Last update check timestamp
    /// </summary>
    public DateTime? LastCheck { get; set; }
}
