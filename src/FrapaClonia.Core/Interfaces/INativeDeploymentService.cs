namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for native executable deployment of frpc
/// </summary>
public interface INativeDeploymentService
{
    /// <summary>
    /// Deploys frpc from a downloaded archive
    /// </summary>
    Task<string> DeployFromArchiveAsync(string archivePath, string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a downloaded frpc binary
    /// </summary>
    Task<bool> VerifyBinaryAsync(string binaryPath, string? expectedChecksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default deployment directory
    /// </summary>
    string GetDefaultDeploymentDirectory();

    /// <summary>
    /// Checks if frpc is already deployed
    /// </summary>
    Task<bool> IsDeployedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deployed frpc binary path
    /// </summary>
    Task<string?> GetDeployedBinaryPathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets executable permissions on the frpc binary (Unix-like systems)
    /// </summary>
    Task SetExecutablePermissionsAsync(string binaryPath, CancellationToken cancellationToken = default);
}
