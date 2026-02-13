namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing frpc versions
/// </summary>
public interface IFrpcVersionService
{
    /// <summary>
    /// Gets available frpc versions from GitHub
    /// </summary>
    Task<IReadOnlyList<FrpcVersionInfo>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest frpc version
    /// </summary>
    Task<FrpcVersionInfo?> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version of a frpc binary
    /// </summary>
    Task<FrpcVersionInfo?> GetBinaryVersionAsync(string binaryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the platform-specific download URL for a version
    /// </summary>
    string? GetDownloadUrl(FrpcVersionInfo version);
}

/// <summary>
/// Information about a frpc version
/// </summary>
public class FrpcVersionInfo
{
    /// <summary>
    /// Version number (e.g., "0.62.1")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Git tag name (e.g., "v0.62.1")
    /// </summary>
    public required string TagName { get; init; }

    /// <summary>
    /// When this version was published
    /// </summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Platform-specific download URL
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Whether this is the latest version
    /// </summary>
    public bool IsLatest { get; set; }

    /// <summary>
    /// Display text for UI
    /// </summary>
    public string DisplayText => IsLatest ? $"{Version} (latest)" : Version;
}
