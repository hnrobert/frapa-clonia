using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for native executable deployment of frpc
/// </summary>
public class NativeDeploymentService : INativeDeploymentService
{
    private readonly ILogger<NativeDeploymentService> _logger;

    public NativeDeploymentService(ILogger<NativeDeploymentService> logger)
    {
        _logger = logger;
    }

    public Task<string> DeployFromArchiveAsync(string archivePath, string targetDirectory, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Deploying frpc from archive {ArchivePath}", archivePath);
        return Task.FromResult(string.Empty);
    }

    public Task<bool> VerifyBinaryAsync(string binaryPath, string? expectedChecksum = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Verifying frpc binary at {BinaryPath}", binaryPath);
        return Task.FromResult(true);
    }

    public string GetDefaultDeploymentDirectory()
    {
        // TODO: Implement cross-platform path in Phase 3
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FrapaClonia", "bin");
    }

    public Task<bool> IsDeployedAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult(false);
    }

    public Task<string?> GetDeployedBinaryPathAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult<string?>(null);
    }

    public Task SetExecutablePermissionsAsync(string binaryPath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3 (Unix-like systems only)
        _logger.LogInformation("Setting executable permissions on {BinaryPath}", binaryPath);
        return Task.CompletedTask;
    }
}
