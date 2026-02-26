using System.Runtime.InteropServices;
using System.Text.Json;
using FrapaClonia.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for Docker deployment of frpc
/// </summary>
public class DockerDeploymentService(ILogger<DockerDeploymentService> logger) : IDockerDeploymentService
{
    private static readonly HttpClient HttpClient = new();

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

            // The caller may pass either a directory (preferred) or a full file path.
            var composePath = ResolveComposeFilePath(outputPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(composePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(composePath, composeContent, cancellationToken);
            logger.LogInformation("docker-compose.yml generated successfully");

            return composePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating docker-compose.yml at {OutputPath}", outputPath);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableImageTagsAsync(string imageRepository,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageRepository))
            {
                return Array.Empty<string>();
            }

            // Only support Docker Hub tags for now.
            var normalized = NormalizeDockerHubRepository(imageRepository);
            if (normalized == null)
            {
                logger.LogWarning("Image repository '{Image}' is not a supported Docker Hub repository",
                    imageRepository);
                return [];
            }

            var (namespaceName, repoName) = normalized.Value;
            var tags = new List<string>();
            var nextUrl = $"https://hub.docker.com/v2/repositories/{namespaceName}/{repoName}/tags?page_size=100";

            // Cap pagination to avoid excessive requests.
            for (var page = 0; page < 20 && !string.IsNullOrEmpty(nextUrl); page++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (document.RootElement.TryGetProperty("results", out var results) &&
                    results.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        if (!item.TryGetProperty("name", out var nameElement) ||
                            nameElement.ValueKind != JsonValueKind.String) continue;

                        var tag = nameElement.GetString();
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            tags.Add(tag);
                        }
                    }
                }

                nextUrl = null;
                if (document.RootElement.TryGetProperty("next", out var nextElement) &&
                    nextElement.ValueKind == JsonValueKind.String)
                {
                    nextUrl = nextElement.GetString();
                }
            }

            return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (OperationCanceledException)
        {
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error fetching tags for image repository '{Image}'", imageRepository);
            return Array.Empty<string>();
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

            var (fileName, argsPrefix) = await GetComposeInvocationAsync(cancellationToken);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = string.IsNullOrEmpty(argsPrefix)
                        ? $"-f \"{composeFile}\" up -d"
                        : $"{argsPrefix} -f \"{composeFile}\" up -d",
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
                logger.LogError("docker-compose start failed: {Error}",
                    await process.StandardError.ReadToEndAsync(cancellationToken));
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

            var (fileName, argsPrefix) = await GetComposeInvocationAsync(cancellationToken);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = string.IsNullOrEmpty(argsPrefix)
                        ? $"-f \"{composeFile}\" down"
                        : $"{argsPrefix} -f \"{composeFile}\" down",
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
                logger.LogWarning("docker-compose stop failed: {Error}",
                    await process.StandardError.ReadToEndAsync(cancellationToken));
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

            var output = (await process.StandardOutput.ReadToEndAsync(cancellationToken)).Trim();
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

    public async Task<bool> IsContainerNameAvailableAsync(string containerName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                return false;
            }

            // List all container names and check exact match.
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerCommand(),
                    Arguments = "ps -a --format \"{{.Names}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogWarning("Docker returned non-zero exit code while listing containers: {ExitCode}",
                    process.ExitCode);
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var existingNames = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return !existingNames.Any(n => string.Equals(n, containerName.Trim(), StringComparison.Ordinal));
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking container name availability for {ContainerName}", containerName);
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

    private static async Task<(string FileName, string ArgsPrefix)> GetComposeInvocationAsync(
        CancellationToken cancellationToken)
    {
        // Prefer the v2 plugin: `docker compose`
        try
        {
            var probe = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetDockerCommand(),
                    Arguments = "compose version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            probe.Start();
            await probe.WaitForExitAsync(cancellationToken);
            if (probe.ExitCode == 0)
            {
                return (GetDockerCommand(), "compose");
            }
        }
        catch
        {
            // ignore and fall back
        }

        // Fall back to legacy standalone binary: `docker-compose`
        return (GetDockerComposeCommand(), "");
    }

    private static string ResolveComposeFilePath(string outputPath)
    {
        var isYamlFile = outputPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                         outputPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);

        return isYamlFile
            ? outputPath
            : Path.Combine(outputPath, "docker-compose.yml"); // Treat as directory.
    }

    private static (string Namespace, string Repo)? NormalizeDockerHubRepository(string imageRepository)
    {
        // Strip any tag suffix if user accidentally includes it.
        var repo = imageRepository.Trim();

        // If a registry host is included, only accept docker.io (and common aliases).
        // Examples accepted:
        // - fatedier/frpc
        // - docker.io/fatedier/frpc
        // - index.docker.io/fatedier/frpc
        if (repo.StartsWith("docker.io/", StringComparison.OrdinalIgnoreCase))
        {
            repo = repo["docker.io/".Length..];
        }
        else if (repo.StartsWith("index.docker.io/", StringComparison.OrdinalIgnoreCase))
        {
            repo = repo["index.docker.io/".Length..];
        }
        else if (repo.Contains('.') && repo.Contains('/'))
        {
            // Some other registry (ghcr.io, quay.io, etc.)
            return null;
        }

        // Remove tag (last ':' after last '/')
        var lastSlash = repo.LastIndexOf('/');
        var lastColon = repo.LastIndexOf(':');
        if (lastColon > lastSlash)
        {
            repo = repo[..lastColon];
        }

        if (string.IsNullOrWhiteSpace(repo)) return null;

        var parts = repo.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => ("library", parts[0]),
            2 => (parts[0], parts[1]),
            _ => null
        };
    }

    private static string GenerateDockerComposeContent(FrpcDockerConfig config)
    {
        var sb = new System.Text.StringBuilder();

        var containerName = string.IsNullOrWhiteSpace(config.ContainerName)
            ? "frapa-clonia-frpc"
            : config.ContainerName.Trim();

        sb.AppendLine("version: '3'");
        sb.AppendLine();
        sb.AppendLine("services:");
        sb.AppendLine($"  {containerName}:");
        sb.AppendLine($"    image: {config.ImageName}:{config.Tag}");
        sb.AppendLine($"    container_name: {containerName}");
        var restart = string.IsNullOrWhiteSpace(config.RestartPolicy)
            ? "unless-stopped"
            : config.RestartPolicy.Trim();
        sb.AppendLine("    restart: " + (string.Equals(restart, "no", StringComparison.OrdinalIgnoreCase)
            ? "\"no\""
            : restart));
        sb.AppendLine("    volumes:");
        // Keep frpc config path simple and portable: always mount ./frpc.toml next to docker-compose.yml
        sb.AppendLine("      - ./frpc.toml:/etc/frp/frpc.toml:ro");

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