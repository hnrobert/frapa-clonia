using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing frpc versions
/// </summary>
public class FrpcVersionService(ILogger<FrpcVersionService> logger) : IFrpcVersionService
{
    private readonly GitHubClient _gitHubClient = new(new ProductHeaderValue("FrapaClonia"));

    public async Task<IReadOnlyList<FrpcVersionInfo>> GetAvailableVersionsAsync()
    {
        try
        {
            logger.LogInformation("Fetching available frpc versions from GitHub");
            var releases = await _gitHubClient.Repository.Release.GetAll("fatedier", "frp");

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
            logger.LogError(ex, "Error fetching available versions from GitHub");
            return new List<FrpcVersionInfo>();
        }
    }


    public async Task<FrpcVersionInfo?> GetBinaryVersionAsync(string binaryPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(binaryPath))
        {
            logger.LogWarning("Binary not found at {Path}", binaryPath);
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

            logger.LogWarning("Could not parse version from output: {Output}", output);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting binary version from {Path}", binaryPath);
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

        return
            $"https://github.com/fatedier/frp/releases/download/{version.TagName}/frp_{version.Version}_{platform}_{arch}{extension}";
    }

    private static string? GetPlatformDownloadUrl(Release release)
    {
        var platform = GetPlatformIdentifier();
        var arch = GetArchitectureIdentifier();
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";

        // Expected asset name pattern: frp_{version}_{platform}_{arch}.{extension}
        // e.g., frp_0.67.0_darwin_arm64.tar.gz

        // Find matching asset - try multiple patterns
        var asset = release.Assets.FirstOrDefault(a =>
            a.Name.Contains($"_{platform}_") &&
            a.Name.Contains($"_{arch}.") && // Note the dot to ensure exact arch match
            a.Name.EndsWith(extension));

        // Fallback: try without the dot
        asset ??= release.Assets.FirstOrDefault(a =>
            a.Name.Contains($"_{platform}_") &&
            a.Name.Contains($"_{arch}") &&
            a.Name.EndsWith(extension));

        // Fallback: just check for platform and arch anywhere in name
        asset ??= release.Assets.FirstOrDefault(a =>
            a.Name.Contains(platform) &&
            a.Name.Contains(arch) &&
            (a.Name.EndsWith(".tar.gz") || a.Name.EndsWith(".zip")));

        if (asset != null)
        {
            return asset.BrowserDownloadUrl;
        }

        // Construct URL as last resort
        var version = release.TagName.TrimStart('v');
        return
            $"https://github.com/fatedier/frp/releases/download/{release.TagName}/frp_{version}_{platform}_{arch}{extension}";
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