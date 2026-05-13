using System.Diagnostics;
using System.Runtime.InteropServices;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Utils;
using Microsoft.Extensions.Logging;
using Octokit;

namespace FrapaClonia.Core.Services;

public class UpdateService(ILogger<UpdateService> logger, ICacheService? cacheService) : IUpdateService
{
    private readonly GitHubClient _gitHubClient = new(new ProductHeaderValue("FrapaClonia"));
    private static readonly HttpClient HttpClient = new();

    private const string Owner = "hnrobert";
    private const string Repo = "frapa-clonia";

    public string CurrentVersion { get; } = AppVersion.Version;

    public UpdateService() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateService>.Instance, null!)
    {
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            logger.LogDebug("Checking for updates (current: {Version})", CurrentVersion);

            var currentStr = CurrentVersion.Contains('+')
                ? CurrentVersion[..CurrentVersion.IndexOf('+')]
                : CurrentVersion;
            if (!Version.TryParse(currentStr, out var current))
            {
                logger.LogWarning("Could not parse current version: {Current}", CurrentVersion);
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
                if (asset == null)
                {
                    logger.LogDebug("No platform asset found for release {Tag}, skipping", release.TagName);
                    continue;
                }

                var info = new AppUpdateInfo
                {
                    Version = versionStr,
                    TagName = release.TagName,
                    HtmlUrl = release.HtmlUrl,
                    ReleaseNotes = release.Body,
                    PublishedAt = release.PublishedAt ?? DateTimeOffset.MinValue,
                    DownloadUrl = asset.BrowserDownloadUrl,
                    DownloadFileName = asset.Name,
                    DownloadSize = asset.Size,
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
                logger.LogDebug("Stable update available: {Version}", stableUpdate.Version);
            if (prereleaseUpdate != null)
                logger.LogDebug("Prerelease update available: {Version}", prereleaseUpdate.Version);
            if (stableUpdate == null && prereleaseUpdate == null)
                logger.LogDebug("App is up to date ({Current})", CurrentVersion);

            return new UpdateCheckResult
            {
                StableUpdate = stableUpdate,
                PrereleaseUpdate = prereleaseUpdate
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking for updates");
            throw;
        }
    }

    public bool IsInstalledViaPackage()
    {
        var exePath = Environment.ProcessPath ?? "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return exePath.StartsWith(pf, StringComparison.OrdinalIgnoreCase)
                   || exePath.StartsWith(pfx86, StringComparison.OrdinalIgnoreCase);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return exePath.StartsWith("/Applications/", StringComparison.Ordinal);
        // Linux
        return exePath.StartsWith("/usr/", StringComparison.Ordinal)
               || exePath.StartsWith("/opt/", StringComparison.Ordinal);
    }

    public async Task<string?> DownloadUpdateAsync(AppUpdateInfo updateInfo, IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(updateInfo.DownloadUrl) || string.IsNullOrEmpty(updateInfo.DownloadFileName))
        {
            logger.LogWarning("No download URL or filename in update info");
            return null;
        }

        var destPath = Path.Combine(Path.GetTempPath(), updateInfo.DownloadFileName);
        logger.LogDebug("Downloading update to {Path}", destPath);

        using var response = await HttpClient.GetAsync(updateInfo.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.DownloadSize;
        await using var src = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var dest = new FileStream(destPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None,
            81920, true);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await src.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;
            if (totalBytes > 0)
                progress?.Report((double)downloaded / totalBytes);
        }

        logger.LogDebug("Download complete: {Path}", destPath);
        return destPath;
    }

    public Task ApplyUpdateAsync(string downloadedFilePath, AppUpdateInfo updateInfo)
    {
        var appDir = Path.GetDirectoryName(Environment.ProcessPath ?? "") ?? "";
        var isInstalled = IsInstalledViaPackage();
        var fileName = updateInfo.DownloadFileName ?? "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ApplyUpdateWindows(downloadedFilePath, fileName, appDir, isInstalled);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ApplyUpdateMacOs(downloadedFilePath);
        }
        else
        {
            ApplyUpdateLinux(downloadedFilePath, appDir);
        }

        return Task.CompletedTask;
    }

    private void ApplyUpdateWindows(string filePath, string fileName, string appDir, bool isInstalled)
    {
        string scriptPath;
        string scriptContent;

        if (isInstalled && fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
        {
            scriptPath = Path.Combine(Path.GetTempPath(), "frapaclonia_update.bat");
            scriptContent = $"""
                             @echo off
                             msiexec /i "{filePath}" /passive /norestart
                             del "%~f0"
                             """;
        }
        else
        {
            // Portable zip update
            scriptPath = Path.Combine(Path.GetTempPath(), "frapaclonia_update.bat");
            scriptContent = $"""
                             @echo off
                             timeout /t 2 /nobreak >nul
                             powershell -Command "Expand-Archive -Path '{filePath}' -DestinationPath '{Path.GetTempPath()}frapaclonia_update_extract' -Force"
                             xcopy /E /Y /I "{Path.GetTempPath()}frapaclonia_update_extract\*" "{appDir}\"
                             start "" "{Path.Combine(appDir, "FrapaClonia.exe")}"
                             rmdir /S /Q "{Path.GetTempPath()}frapaclonia_update_extract"
                             del "{filePath}"
                             del "%~f0"
                             """;
        }

        File.WriteAllText(scriptPath, scriptContent);
        logger.LogDebug("Launching Windows update script: {Script}", scriptPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private void ApplyUpdateMacOs(string filePath)
    {
        // macOS has no portable distribution — always a DMG that installs to /Applications.
        var scriptPath = Path.Combine(Path.GetTempPath(), "frapaclonia_update.sh");
        var scriptContent = $$"""
                              #!/bin/bash
                              sleep 2
                              MOUNT=$(hdiutil attach "{{filePath}}" | tail -1 | awk '{print $NF}')
                              cp -R "$MOUNT"/*.app /Applications/
                              hdiutil detach "$MOUNT"
                              open /Applications/FrapaClonia.app
                              rm -f "{{filePath}}"
                              rm -f "$0"
                              """;

        File.WriteAllText(scriptPath, scriptContent);
        Process.Start(new ProcessStartInfo("chmod", $"+x \"{scriptPath}\"") { UseShellExecute = false })?.WaitForExit();
        logger.LogDebug("Launching macOS update script: {Script}", scriptPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private void ApplyUpdateLinux(string filePath, string appDir)
    {
        var extractDir = Path.Combine(Path.GetTempPath(), "frapaclonia_update_extract");
        var scriptPath = Path.Combine(Path.GetTempPath(), "frapaclonia_update.sh");
        var scriptContent = $"""
                             #!/bin/bash
                             sleep 2
                             mkdir -p "{extractDir}"
                             tar -xzf "{filePath}" -C "{extractDir}"
                             cp -R "{extractDir}"/* "{appDir}/"
                             "{Path.Combine(appDir, "FrapaClonia")}" &
                             rm -rf "{extractDir}"
                             rm -f "{filePath}"
                             rm -f "$0"
                             """;

        File.WriteAllText(scriptPath, scriptContent);
        Process.Start(new ProcessStartInfo("chmod", $"+x \"{scriptPath}\"") { UseShellExecute = false })?.WaitForExit();
        logger.LogDebug("Launching Linux update script: {Script}", scriptPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private void ApplyToken()
    {
        var token = cacheService?.GitHubToken;
        if (!string.IsNullOrEmpty(token))
        {
            _gitHubClient.Credentials = new Credentials(token);
        }
    }

    private static ReleaseAsset? FindPlatformAsset(Release release)
    {
        var (platform, extension) = GetPlatformInfo();

        // Asset names end with "-{platform}.ext" or "-{platform}-installer.ext",
        // so match Contains("-{platform}") rather than requiring a trailing dash.
        return release.Assets.FirstOrDefault(a =>
            a.Name.Contains($"-{platform}") && a.Name.EndsWith(extension));
    }

    private static (string platform, string extension) GetPlatformInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Artifacts are renamed win-x64 → windows-x64 in CI.
            var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            return ($"windows-{arch}", ".msi");
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
