using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for Docker deployment of frpc
/// </summary>
public class DockerDeploymentService(ILogger<DockerDeploymentService> logger) : IDockerDeploymentService
{
    public async Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking if Docker is available");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerCommand(),
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var available = process.ExitCode == 0;
            logger.LogInformation("Docker is {Status}", available ? "available" : "not available");

            return available;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking Docker availability");
            return false;
        }
    }

    public async Task<string> GenerateDockerComposeAsync(string outputPath, FrpcDockerConfig config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Generating docker-compose.yml at {OutputPath}", outputPath);

            var composeContent = GenerateDockerComposeContent(config);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(outputPath, composeContent, cancellationToken);
            logger.LogInformation("docker-compose.yml generated successfully");

            return outputPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating docker-compose.yml at {OutputPath}", outputPath);
            throw;
        }
    }

    public async Task<bool> StartDockerComposeAsync(string composeDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting docker-compose in {Directory}", composeDirectory);

            var composeFile = Path.Combine(composeDirectory, "docker-compose.yml");
            if (!File.Exists(composeFile))
            {
                logger.LogError("docker-compose.yml not found in {Directory}", composeDirectory);
                return false;
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerComposeCommand(),
                    Arguments = $"-f \"{composeFile}\" up -d",
                    WorkingDirectory = composeDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var success = process.ExitCode == 0;
            if (success)
            {
                logger.LogInformation("docker-compose started successfully");
            }
            else
            {
                logger.LogError("docker-compose start failed: {Error}", process.StandardError.ReadToEnd());
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting docker-compose in {Directory}", composeDirectory);
            return false;
        }
    }

    public async Task<bool> StopDockerComposeAsync(string composeDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Stopping docker-compose in {Directory}", composeDirectory);

            var composeFile = Path.Combine(composeDirectory, "docker-compose.yml");
            if (!File.Exists(composeFile))
            {
                logger.LogWarning("docker-compose.yml not found in {Directory}", composeDirectory);
                return false;
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerComposeCommand(),
                    Arguments = $"-f \"{composeFile}\" down",
                    WorkingDirectory = composeDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var success = process.ExitCode == 0;
            if (success)
            {
                logger.LogInformation("docker-compose stopped successfully");
            }
            else
            {
                logger.LogWarning("docker-compose stop failed: {Error}", process.StandardError.ReadToEnd());
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping docker-compose in {Directory}", composeDirectory);
            return false;
        }
    }

    public async Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Checking if container {ContainerName} is running", containerName);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerCommand(),
                    Arguments = $"ps -q -f name={containerName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var output = process.StandardOutput.ReadToEnd().Trim();
            var isRunning = !string.IsNullOrEmpty(output);

            logger.LogInformation("Container {ContainerName} is {Status}",
                containerName, isRunning ? "running" : "not running");

            return isRunning;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking container {ContainerName} status", containerName);
            return false;
        }
    }

    private static string GetDockerCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker.exe" : "docker";
    }

    private static string GetDockerComposeCommand()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker-compose.exe" : "docker-compose";
    }

    private static string GenerateDockerComposeContent(FrpcDockerConfig config)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("version: '3'");
        sb.AppendLine();
        sb.AppendLine("services:");
        sb.AppendLine("  frpa-clonia-frpc:");
        sb.AppendLine($"    image: {config.ImageName}:{config.Tag}");
        sb.AppendLine("    container_name: frapa-clonia-frpc");
        sb.AppendLine("    restart: " + (config.AutoRestart ? "always" : "\"no\""));
        sb.AppendLine("    volumes:");
        sb.AppendLine($"      - {Path.GetFullPath(config.ConfigPath)}:/etc/frp/frpc.toml:ro");

        // Add environment variables
        if (config.EnvironmentVariables.Count > 0)
        {
            sb.AppendLine("    environment:");
            foreach (var kvp in config.EnvironmentVariables)
            {
                sb.AppendLine($"      - {kvp.Key}={kvp.Value}");
            }
        }

        // Add ports
        if (config.Ports.Count > 0)
        {
            sb.AppendLine("    ports:");
            foreach (var port in config.Ports)
            {
                sb.AppendLine($"      - \"{port}\"");
            }
        }

        sb.AppendLine();
        sb.AppendLine("# Generated by FrapaClonia - frpc visual client");

        return sb.ToString();
    }
}