namespace FrapaClonia.Shared.Models;

/// <summary>
/// Application cache data stored in cache.toml
/// </summary>
public class AppCache
{
    /// <summary>
    /// Current preset information
    /// </summary>
    public CurrentPresetInfo CurrentPreset { get; } = new();

    /// <summary>
    /// Application information
    /// </summary>
    public AppInfo App { get; } = new();
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
    public DateTime? LastSelfUpdateCheck { get; set; }

    /// <summary>
    /// Last Frpc version check timestamp
    /// </summary>
    public DateTime? LastFrpcVersionCheck { get; set; }

    /// <summary>
    /// GitHub personal access token for higher API rate limits
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// Last GitHub Token validation timestamp
    /// </summary>
    public DateTime? LastGitHubTokenValidation { get; set; }

    /// <summary>
    /// Cached frpc version list from GitHub
    /// </summary>
    public List<CachedFrpcVersion> FrpcVersions { get; } = [];
}

/// <summary>
/// Cached frpc version entry for TOML serialization
/// </summary>
public class CachedFrpcVersion
{
    public string Version { get; init; } = "";
    public string TagName { get; init; } = "";
    public DateTimeOffset PublishedAt { get; init; }
    public string? DownloadUrl { get; init; }
    public bool IsLatest { get; init; }
}
