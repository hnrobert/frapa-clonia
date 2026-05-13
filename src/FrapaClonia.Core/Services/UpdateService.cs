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

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            _logger.LogDebug("Checking for updates (current: {Version})", CurrentVersion);

            var currentStr = CurrentVersion.Contains('+') ? CurrentVersion[..CurrentVersion.IndexOf('+')] : CurrentVersion;
            if (!Version.TryParse(currentStr, out var current))
            {
                _logger.LogWarning("Could not parse current version: {Current}", CurrentVersion);
                return new UpdateCheckResult();
            }

            ApplyToken();
            var releases = await _gitHubClient.Repository.Release.GetAll(Owner, Repo);

            AppUpdateInfo? stableUpdate = null;
            AppUpdateInfo? prereleaseUpdate = null;

            foreach (var release in releases)
            {
                var versionStr = release.TagName.TrimStart('v');
                if (!Version.TryParse(versionStr, out var version)) continue;
                if (version <= current) continue;

                var asset = FindPlatformAsset(release);
                var info = new AppUpdateInfo
                {
                    Version = versionStr,
                    TagName = release.TagName,
                    HtmlUrl = release.HtmlUrl,
                    ReleaseNotes = release.Body,
                    PublishedAt = release.PublishedAt ?? DateTimeOffset.MinValue,
                    DownloadUrl = asset?.BrowserDownloadUrl,
                    DownloadFileName = asset?.Name,
                    DownloadSize = asset?.Size ?? 0,
                    IsPrerelease = release.Prerelease
                };

                if (release.Prerelease)
                {
                    prereleaseUpdate ??= info;
                }
                else
                {
                    stableUpdate ??= info;
                    // Found the latest stable, no need to look further for stable
                    // But keep looking for prerelease if not found yet
                    if (prereleaseUpdate != null) break;
                }
            }

            if (stableUpdate != null)
                _logger.LogDebug("Stable update available: {Version}", stableUpdate.Version);
            if (prereleaseUpdate != null)
                _logger.LogDebug("Prerelease update available: {Version}", prereleaseUpdate.Version);
            if (stableUpdate == null && prereleaseUpdate == null)
                _logger.LogDebug("App is up to date ({Current})", CurrentVersion);

            return new UpdateCheckResult
            {
                StableUpdate = stableUpdate,
                PrereleaseUpdate = prereleaseUpdate
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
            return ($"win-{arch}", ".msi");
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
