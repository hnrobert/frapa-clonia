namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for detecting and using package managers to install frpc
/// </summary>
public interface IPackageManagerService
{
    /// <summary>
    /// Detects available package managers on the current system
    /// </summary>
    Task<IReadOnlyList<PackageManagerInfo>> DetectAvailablePackageManagersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific package manager is installed
    /// </summary>
    Task<bool> IsPackageManagerInstalledAsync(string packageManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs frpc using the specified package manager
    /// </summary>
    Task<bool> InstallFrpcAsync(string packageManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the frpc binary path if installed via package manager
    /// </summary>
    Task<string?> GetFrpcBinaryPathAsync(string packageManager, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls frpc using the specified package manager
    /// </summary>
    Task<bool> UninstallFrpcAsync(string packageManager, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a package manager
/// </summary>
public class PackageManagerInfo
{
    /// <summary>
    /// Internal identifier (e.g., "brew", "scoop", "choco")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Display name (e.g., "Homebrew", "Scoop", "Chocolatey")
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Whether this package manager is installed on the system
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Whether frpc can be installed via this package manager
    /// </summary>
    public bool CanInstallFrpc { get; set; }

    /// <summary>
    /// Command to install the package manager (for guidance if not installed)
    /// </summary>
    public string? InstallCommand { get; init; }

    /// <summary>
    /// Command to install frpc via this package manager
    /// </summary>
    public string? FrpcInstallCommand { get; init; }

    /// <summary>
    /// Platform this package manager runs on (macOS, windows, linux)
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// Linux distribution if applicable (debian, arch, alpine, fedora, etc.)
    /// </summary>
    public string? LinuxDistro { get; init; }
}
