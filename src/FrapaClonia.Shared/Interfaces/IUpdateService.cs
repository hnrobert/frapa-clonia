namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Service for checking and applying application updates
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks GitHub for a newer release. Returns null if up-to-date.
    /// </summary>
    Task<AppUpdateInfo?> CheckForUpdatesAsync();

    /// <summary>
    /// The current application version
    /// </summary>
    string CurrentVersion { get; }
}

/// <summary>
/// Information about an available app update
/// </summary>
public class AppUpdateInfo
{
    public required string Version { get; init; }
    public required string TagName { get; init; }
    public string? HtmlUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string? DownloadUrl { get; init; }
    public string? DownloadFileName { get; init; }
    public long DownloadSize { get; init; }
}
