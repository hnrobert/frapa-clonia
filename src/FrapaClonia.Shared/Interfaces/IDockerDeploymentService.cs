namespace FrapaClonia.Shared.Interfaces;

public enum DockerContainerStatus
{
    NotFound,
    Running,
    Stopped,
    Restarting,
    Other
}

/// <summary>
/// Service for Docker deployment of frpc
/// </summary>
public interface IDockerDeploymentService
{
    Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateDockerComposeAsync(string outputPath, FrpcDockerConfig config,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAvailableImageTagsAsync(string imageRepository,
        CancellationToken cancellationToken = default);

    Task<bool> StartDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default);

    Task<bool> RecreateDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default);

    Task<bool> StopDockerComposeAsync(string composeDirectory, CancellationToken cancellationToken = default);

    Task<DockerContainerStatus> GetContainerStatusAsync(string containerName,
        CancellationToken cancellationToken = default);

    Task<bool> StartContainerAsync(string containerName, CancellationToken cancellationToken = default);

    Task<bool> StopContainerAsync(string containerName, CancellationToken cancellationToken = default);

    Task<bool> RestartContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether frpc Docker container is running
    /// </summary>
    Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default);

    Task<bool> IsContainerNameAvailableAsync(string containerName, CancellationToken cancellationToken = default);

    Task<bool> IsContainerOwnedByComposeAsync(string composeDirectory, string containerName,
        CancellationToken cancellationToken = default);
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

    /// <summary>
    /// Docker compose restart policy: "no", "always", "on-failure", "unless-stopped"
    /// </summary>
    public required string RestartPolicy { get; init; } = "unless-stopped";

    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
    public List<string> Ports { get; init; } = new();
}
