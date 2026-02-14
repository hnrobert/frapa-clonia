namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Information about a downloaded frpc version
/// </summary>
public class DownloadedFrpcVersion
{
    public string Version { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Architecture { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public string BinaryPath { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset DownloadedAt { get; set; }
    public bool IsInUse { get; set; }
}

/// <summary>
/// Service for native executable deployment of frpc
/// </summary>
public interface INativeDeploymentService
{
    /// <summary>
    /// Deploys frpc from a downloaded archive to a versioned folder
    /// </summary>
    /// <param name="archivePath">Path to the downloaded archive</param>
    /// <param name="version">The version string (e.g., "0.62.1")</param>
    /// <param name="platform">The platform (e.g., "darwin", "windows", "linux")</param>
    /// <param name="architecture">The architecture (e.g., "arm64", "amd64")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the deployed binary</returns>
    Task<string> DeployFromArchiveAsync(string archivePath, string version, string platform, string architecture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a downloaded frpc binary
    /// </summary>
    Task<bool> VerifyBinaryAsync(string binaryPath, string? expectedChecksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default deployment directory (bin folder)
    /// </summary>
    string GetDefaultDeploymentDirectory();

    /// <summary>
    /// Checks if frpc is already deployed
    /// </summary>
    Task<bool> IsDeployedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deployed frpc binary path (returns first available or empty)
    /// </summary>
    Task<string> GetDeployedBinaryPathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets executable permissions on the frpc binary (Unix-like systems)
    /// </summary>
    Task SetExecutablePermissionsAsync(string binaryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all downloaded frpc versions
    /// </summary>
    Task<IReadOnlyList<DownloadedFrpcVersion>> GetDownloadedVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a downloaded version folder
    /// </summary>
    Task<bool> DeleteVersionAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version folder name for a specific version/platform/arch
    /// </summary>
    string GetVersionFolderName(string version, string platform, string architecture);

    /// <summary>
    /// Gets the binary path for a specific version
    /// </summary>
    string GetBinaryPathForVersion(string version, string platform, string architecture);
}
