using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Runtime.InteropServices;
using IO = System.IO;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for downloading frpc binaries
/// </summary>
public class FrpcDownloader(ILogger<FrpcDownloader> logger) : IFrpcDownloader
{
    private static readonly HttpClient HttpClient = new();
    private readonly GitHubClient _gitHubClient = new(new ProductHeaderValue("FrapaClonia"));

    public async Task<IReadOnlyList<FrpRelease>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching available frpc versions from GitHub");
            var releases = await _gitHubClient.Repository.Release.GetAll("fatedier", "frp");

            return releases.Select(r => new FrpRelease
            {
                TagName = r.TagName,
                Version = r.TagName.TrimStart('v'),
                HtmlUrl = r.HtmlUrl,
                PublishedAt = r.PublishedAt ?? DateTimeOffset.MinValue,
                Assets = r.Assets.Select(a => new FrpAsset
                {
                    Name = a.Name,
                    DownloadUrl = a.BrowserDownloadUrl,
                    Size = a.Size,
                    Platform = GetPlatformFromAssetName(a.Name),
                    Architecture = GetArchitectureFromAssetName(a.Name)
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching available versions from GitHub");
            return new List<FrpRelease>();
        }
    }

    public async Task<string> DownloadVersionAsync(FrpRelease release, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var platform = GetPlatformIdentifier();
        var arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

        // Find the appropriate asset
        var asset = release.Assets.FirstOrDefault(a =>
            a.Platform == platform && a.Architecture.Contains(arch));

        if (asset == null)
        {
            logger.LogWarning("Could not find matching asset for platform {Platform} and architecture {Arch}", platform, arch);
            // Try to find any compatible asset
            asset = release.Assets.FirstOrDefault(a => a.Platform == platform)
                ?? release.Assets.FirstOrDefault();
        }

        if (asset == null)
        {
            throw new InvalidOperationException($"No suitable asset found in release {release.TagName}");
        }

        logger.LogInformation("Downloading frpc {Version} for {Platform}-{Arch} from {Url}",
            release.Version, platform, arch, asset.DownloadUrl);

        // Ensure target directory exists
        Directory.CreateDirectory(targetDirectory);

        var fileName = asset.Name;
        var filePath = Path.Combine(targetDirectory, fileName);

        // Download the file
        var response = await HttpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[8192];
        var totalRead = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(filePath, IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                progress.Report((double)totalRead / totalBytes * 100);
            }
        }

        logger.LogInformation("Downloaded frpc to {FilePath}", filePath);
        return filePath;
    }

    public async Task<FrpRelease?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching latest frpc version from GitHub");
            var latest = await _gitHubClient.Repository.Release.GetLatest("fatedier", "frp");

            return new FrpRelease
            {
                TagName = latest.TagName,
                Version = latest.TagName.TrimStart('v'),
                HtmlUrl = latest.HtmlUrl,
                PublishedAt = latest.PublishedAt ?? DateTimeOffset.MinValue,
                Assets = latest.Assets.Select(a => new FrpAsset
                {
                    Name = a.Name,
                    DownloadUrl = a.BrowserDownloadUrl,
                    Size = a.Size,
                    Platform = GetPlatformFromAssetName(a.Name),
                    Architecture = GetArchitectureFromAssetName(a.Name)
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching latest version from GitHub");
            return null;
        }
    }

    public async Task<string> DownloadFromMirrorAsync(string mirrorUrl, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Downloading frpc from mirror {MirrorUrl}", mirrorUrl);

            // Ensure target directory exists
            Directory.CreateDirectory(targetDirectory);

            var fileName = Path.GetFileName(mirrorUrl.TrimEnd('/'));
            var filePath = Path.Combine(targetDirectory, fileName);

            // Download the file
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(mirrorUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var totalRead = 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(filePath, IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    progress.Report((double)totalRead / totalBytes * 100);
                }
            }

            logger.LogInformation("Downloaded frpc from mirror to {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading frpc from mirror {MirrorUrl}", mirrorUrl);
            throw;
        }
    }

    private static string GetPlatformIdentifier()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" :
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
               "unknown";
    }

    private static string GetPlatformFromAssetName(string assetName)
    {
        var lower = assetName.ToLowerInvariant();
        if (lower.Contains("windows") || lower.Contains("win")) return "windows";
        if (lower.Contains("darwin") || lower.Contains("mac") || lower.Contains("osx")) return "darwin";
        if (lower.Contains("linux")) return "linux";
        return "unknown";
    }

    private static List<string> GetArchitectureFromAssetName(string assetName)
    {
        var lower = assetName.ToLowerInvariant();
        var archs = new List<string>();

        if (lower.Contains("amd64") || lower.Contains("x86_64") || lower.Contains("x86-64")) archs.Add("amd64");
        if (lower.Contains("arm64") || lower.Contains("aarch64")) archs.Add("arm64");
        if (lower.Contains("arm") && !lower.Contains("arm64")) archs.Add("arm");
        if (lower.Contains("386") || lower.Contains("i386")) archs.Add("386");
        if (lower.Contains("mips")) archs.Add("mips");

        return archs.Count > 0 ? archs : ["unknown"];
    }
}
