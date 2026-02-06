using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for Docker deployment of frpc
/// </summary>
public class DockerDeploymentService : IDockerDeploymentService
{
    private readonly ILogger<DockerDeploymentService> _logger;

    public DockerDeploymentService(ILogger<DockerDeploymentService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult(false);
    }

    public Task<string> GenerateDockerComposeAsync(string outputPath, FrpcDockerConfig config, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Generating docker-compose.yml at {OutputPath}", outputPath);
        return Task.FromResult(string.Empty);
    }

    public Task<bool> StartDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Starting docker-compose at {Directory}", composeDirectory);
        return Task.FromResult(true);
    }

    public Task<bool> StopDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Stopping docker-compose at {Directory}", composeDirectory);
        return Task.FromResult(true);
    }

    public Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult(false);
    }
}
