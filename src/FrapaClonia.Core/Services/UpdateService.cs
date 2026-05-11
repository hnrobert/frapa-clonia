using System.Runtime.InteropServices;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Utils;
using Microsoft.Extensions.Logging;
using Octokit;

namespace FrapaClonia.Core.Services;

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly ICacheService? _cacheService;
    private readonly GitHubClient _gitHubClient = new(new ProductHeaderValue("FrapaClonia"));

    private const string Owner = "hnrobert";
    private const string Repo = "frapa-clonia";

    public string CurrentVersion { get; }

    public UpdateService() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateService>.Instance, null!)
    {
    }

    public UpdateService(ILogger<UpdateService> logger, ICacheService? cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
        CurrentVersion = AppVersion.Version;
    }

    public async Task<AppUpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for updates (current: {Version})", CurrentVersion);

            ApplyToken();
            var release = await _gitHubClient.Repository.Release.GetLatest(Owner, Repo);

            var latestVersion = release.TagName.TrimStart('v');

            if (!Version.TryParse(latestVersion, out var latest) ||
                !Version.TryParse(CurrentVersion, out var current))
            {
                _logger.LogWarning("Could not parse versions: latest={Latest}, current={Current}", latestVersion,
                    CurrentVersion);
                return null;
            }

            if (latest <= current)
            {
                _logger.LogInformation("App is up to date ({Current})", CurrentVersion);
                return null;
            }

            var asset = FindPlatformAsset(release);

            _logger.LogInformation("Update available: {Latest} (current: {Current})", latestVersion, CurrentVersion);

            return new AppUpdateInfo
            {
                Version = latestVersion,
                TagName = release.TagName,
                HtmlUrl = release.HtmlUrl,
                ReleaseNotes = release.Body,
                PublishedAt = release.PublishedAt ?? DateTimeOffset.MinValue,
                DownloadUrl = asset?.BrowserDownloadUrl,
                DownloadFileName = asset?.Name,
                DownloadSize = asset?.Size ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            throw;
        }
    }

    private void ApplyToken()
    {
        var token = _cacheService?.GitHubToken;
        if (!string.IsNullOrEmpty(token))
        {
            _gitHubClient.Credentials = new Credentials(token);
        }
    }

    private static ReleaseAsset? FindPlatformAsset(Release release)
    {
        var (platform, extension) = GetPlatformInfo();

        return release.Assets.FirstOrDefault(a =>
            a.Name.Contains($"-{platform}-") && a.Name.EndsWith(extension));
    }

    private static (string platform, string extension) GetPlatformInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            return ($"win-{arch}", ".zip");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            return ($"macos-{arch}", ".dmg");
        }

        // Linux
        var linuxArch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64"
        };
        return ($"linux-{linuxArch}", ".tar.gz");
    }
}
