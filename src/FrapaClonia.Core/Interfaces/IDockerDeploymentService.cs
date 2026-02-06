namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for Docker deployment of frpc
/// </summary>
public interface IDockerDeploymentService
{
    /// <summary>
    /// Checks if Docker is available
    /// </summary>
    Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a docker-compose.yml file for frpc
    /// </summary>
    Task<string> GenerateDockerComposeAsync(string outputPath, FrpcDockerConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts frpc using Docker Compose
    /// </summary>
    Task<bool> StartDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops frpc using Docker Compose
    /// </summary>
    Task<bool> StopDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether frpc Docker container is running
    /// </summary>
    Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for Docker deployment
/// </summary>
public class FrpcDockerConfig
{
    public required string ImageName { get; init; } = "fatedier/frpc";
    public required string Tag { get; init; } = "latest";
    public required string ConfigPath { get; init; }
    public required string ContainerName { get; init; } = "frapa-clonia-frpc";
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
    public List<string> Ports { get; init; } = new();
    public bool AutoRestart { get; init; } = true;
}
