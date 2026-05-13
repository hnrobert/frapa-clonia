namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Service for checking and applying application updates
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks GitHub for newer releases. Returns stable and prerelease info.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync();

    /// <summary>
    /// The current application version
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Downloads the update file to a temp path, reporting progress as 0.0–1.0.
    /// Returns the local file path, or null on failure.
    /// </summary>
    Task<string?> DownloadUpdateAsync(AppUpdateInfo updateInfo, IProgress<double>? progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes and launches an OS-appropriate update script, then returns.
    /// The caller is responsible for calling Environment.Exit after this.
    /// </summary>
    Task ApplyUpdateAsync(string downloadedFilePath, AppUpdateInfo updateInfo);

    /// <summary>
    /// Returns true when the app is running from a system-installed location
    /// (e.g. Program Files on Windows, /Applications on macOS, /usr or /opt on Linux).
    /// </summary>
    bool IsInstalledViaPackage();
}

/// <summary>
/// Result of checking for updates
/// </summary>
public class UpdateCheckResult
{
    public AppUpdateInfo? StableUpdate { get; init; }
    public AppUpdateInfo? PrereleaseUpdate { get; init; }
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
    public bool IsPrerelease { get; init; }
}
