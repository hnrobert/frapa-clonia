using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing frpc versions
/// </summary>
public class FrpcVersionService : IFrpcVersionService
{
    private readonly ILogger<FrpcVersionService> _logger;
    private readonly GitHubClient _gitHubClient;

    public FrpcVersionService(ILogger<FrpcVersionService> logger)
    {
        _logger = logger;
        _gitHubClient = new GitHubClient(new ProductHeaderValue("FrapaClonia"));
    }

    public async Task<IReadOnlyList<FrpcVersionInfo>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching available frpc versions from GitHub");
            var releases = await _gitHubClient.Repository.Release.GetAll("fatedier", "frp");

            var latestVersion = releases.FirstOrDefault()?.TagName.TrimStart('v');

            return releases.Select((r, index) => new FrpcVersionInfo
            {
                TagName = r.TagName,
                Version = r.TagName.TrimStart('v'),
                PublishedAt = r.PublishedAt ?? DateTimeOffset.MinValue,
                DownloadUrl = GetPlatformDownloadUrl(r),
                IsLatest = index == 0
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available versions from GitHub");
            return new List<FrpcVersionInfo>();
        }
    }

    public async Task<FrpcVersionInfo?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching latest frpc version from GitHub");
            var latest = await _gitHubClient.Repository.Release.GetLatest("fatedier", "frp");

            return new FrpcVersionInfo
            {
                TagName = latest.TagName,
                Version = latest.TagName.TrimStart('v'),
                PublishedAt = latest.PublishedAt ?? DateTimeOffset.MinValue,
                DownloadUrl = GetPlatformDownloadUrl(latest),
                IsLatest = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest version from GitHub");
            return null;
        }
    }

    public async Task<FrpcVersionInfo?> GetBinaryVersionAsync(string binaryPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(binaryPath))
        {
            _logger.LogWarning("Binary not found at {Path}", binaryPath);
            return null;
        }

        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = "-v",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Parse version from output like "frpc version 0.62.1"
            var versionText = output.Trim();
            var parts = versionText.Split(' ');
            var versionPart = parts.LastOrDefault();

            if (!string.IsNullOrEmpty(versionPart) && Version.TryParse(versionPart, out _))
            {
                return new FrpcVersionInfo
                {
                    Version = versionPart,
                    TagName = $"v{versionPart}",
                    PublishedAt = DateTimeOffset.MinValue,
                    IsLatest = false
                };
            }

            _logger.LogWarning("Could not parse version from output: {Output}", output);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting binary version from {Path}", binaryPath);
            return null;
        }
    }

    public string? GetDownloadUrl(FrpcVersionInfo version)
    {
        if (!string.IsNullOrEmpty(version.DownloadUrl))
        {
            return version.DownloadUrl;
        }

        // Construct URL based on platform
        var platform = GetPlatformIdentifier();
        var arch = GetArchitectureIdentifier();
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";

        return $"https://github.com/fatedier/frp/releases/download/{version.TagName}/frp_{version.Version}_{platform}_{arch}{extension}";
    }

    private static string? GetPlatformDownloadUrl(Release release)
    {
        var platform = GetPlatformIdentifier();
        var arch = GetArchitectureIdentifier();
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";

        // Find matching asset
        var asset = release.Assets.FirstOrDefault(a =>
            a.Name.Contains($"_{platform}_") &&
            a.Name.Contains($"_{arch}") &&
            a.Name.EndsWith(extension));

        return asset?.BrowserDownloadUrl;
    }

    private static string GetPlatformIdentifier()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" :
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
               "unknown";
    }

    private static string GetArchitectureIdentifier()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "386",
            Architecture.Arm => "arm",
            _ => "amd64"
        };
    }
}
