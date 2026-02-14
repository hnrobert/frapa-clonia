using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for native executable deployment of frpc
/// </summary>
public class NativeDeploymentService(ILogger<NativeDeploymentService> logger) : INativeDeploymentService
{
    private static readonly string CurrentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" : "unknown";

    private static readonly string CurrentArchitecture = RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "amd64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "386",
        Architecture.Arm => "arm",
        _ => "amd64"
    };

    public async Task<string> DeployFromArchiveAsync(string archivePath, string version, string platform, string architecture,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Deploying frpc v{Version} ({Platform}/{Architecture}) from {ArchivePath}",
                version, platform, architecture, archivePath);

            // Create versioned folder
            var folderName = GetVersionFolderName(version, platform, architecture);
            var targetDirectory = Path.Combine(GetDefaultDeploymentDirectory(), folderName);
            Directory.CreateDirectory(targetDirectory);

            // Extract the archive
            string binaryPath;
            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                binaryPath = await ExtractTarGzAsync(archivePath, targetDirectory, cancellationToken);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                binaryPath = await ExtractZipAsync(archivePath, targetDirectory, cancellationToken);
            }
            else
            {
                // Assume it's already an uncompressed binary
                var fileName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
                binaryPath = Path.Combine(targetDirectory, fileName);
                File.Copy(archivePath, binaryPath, true);
                await SetExecutablePermissionsAsync(binaryPath, cancellationToken);
            }

            // Clean up the archive file
            try
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                    logger.LogInformation("Deleted archive file: {ArchivePath}", archivePath);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete archive file: {ArchivePath}", archivePath);
            }

            logger.LogInformation("Successfully deployed frpc to: {BinaryPath}", binaryPath);
            return binaryPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deploying frpc from archive {ArchivePath}", archivePath);
            throw;
        }
    }

    public async Task<bool> VerifyBinaryAsync(string binaryPath, string? expectedChecksum = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(binaryPath))
            {
                logger.LogWarning("Binary file does not exist: {BinaryPath}", binaryPath);
                return false;
            }

            logger.LogInformation("Verifying frpc binary at {BinaryPath}", binaryPath);

            // If checksum provided, verify it
            if (!string.IsNullOrEmpty(expectedChecksum))
            {
                var actualChecksum = await ComputeSha256ChecksumAsync(binaryPath, cancellationToken);
                var matches = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);

                if (!matches)
                {
                    logger.LogWarning("Checksum mismatch for {BinaryPath}. Expected: {Expected}, Actual: {Actual}",
                        binaryPath, expectedChecksum, actualChecksum);
                }

                return matches;
            }

            // Verify the binary is executable by checking if we can get file info
            var fileInfo = new FileInfo(binaryPath);
            if (fileInfo.Length == 0)
            {
                logger.LogWarning("Binary file is empty: {BinaryPath}", binaryPath);
                return false;
            }

            // On Unix, try to execute with --version flag
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = binaryPath,
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false
                        }
                    };
                    process.Start();
                    await process.WaitForExitAsync(cancellationToken);

                    // Exit code 0 usually means the binary ran successfully
                    // frpc returns 0 when it can run (even with invalid args)
                    return process.ExitCode == 0 || process.StandardOutput.ReadToEnd().Contains("frpc");
                }
                catch
                {
                    logger.LogWarning("Could not execute {BinaryPath} to verify", binaryPath);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying binary at {BinaryPath}", binaryPath);
            return false;
        }
    }

    public string GetDefaultDeploymentDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FrapaClonia", "bin");
    }

    public async Task<bool> IsDeployedAsync(CancellationToken cancellationToken = default)
    {
        var versions = await GetDownloadedVersionsAsync(cancellationToken);
        return versions.Count > 0;
    }

    public async Task<string> GetDeployedBinaryPathAsync(CancellationToken cancellationToken = default)
    {
        // First, try to get current platform version
        var versions = await GetDownloadedVersionsAsync(cancellationToken);

        // Prefer current platform/arch
        var currentPlatformVersion = versions.FirstOrDefault(v =>
            v.Platform == CurrentPlatform && v.Architecture == CurrentArchitecture);

        if (currentPlatformVersion != null)
        {
            return currentPlatformVersion.BinaryPath;
        }

        // Fallback to any available version
        var anyVersion = versions.FirstOrDefault();
        return anyVersion?.BinaryPath ?? string.Empty;
    }

    public Task SetExecutablePermissionsAsync(string binaryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows doesn't need explicit executable permissions
                return Task.CompletedTask;
            }

            logger.LogInformation("Setting executable permissions on {BinaryPath}", binaryPath);

            // Use chmod +x to make the binary executable
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{binaryPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                logger.LogWarning("chmod failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, process.StandardError.ReadToEnd());
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting executable permissions on {BinaryPath}", binaryPath);
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<DownloadedFrpcVersion>> GetDownloadedVersionsAsync(CancellationToken cancellationToken = default)
    {
        var versions = new List<DownloadedFrpcVersion>();
        var binDir = GetDefaultDeploymentDirectory();

        if (!Directory.Exists(binDir))
        {
            return Task.FromResult<IReadOnlyList<DownloadedFrpcVersion>>(versions);
        }

        var exeName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

        foreach (var folder in Directory.GetDirectories(binDir))
        {
            try
            {
                var folderName = Path.GetFileName(folder);

                // Parse folder name: version_platform_arch (e.g., "0.62.1_darwin_arm64")
                var parts = folderName.Split('_');
                if (parts.Length >= 3)
                {
                    var binaryPath = Path.Combine(folder, exeName);
                    if (File.Exists(binaryPath))
                    {
                        var fileInfo = new FileInfo(binaryPath);
                        var dirInfo = new DirectoryInfo(folder);

                        versions.Add(new DownloadedFrpcVersion
                        {
                            Version = parts[0],
                            Platform = parts[1],
                            Architecture = parts[2],
                            FolderPath = folder,
                            BinaryPath = binaryPath,
                            SizeBytes = fileInfo.Length,
                            DownloadedAt = dirInfo.CreationTimeUtc,
                            IsInUse = false // Will be set by caller if needed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error parsing version folder: {Folder}", folder);
            }
        }

        // Sort by version descending (newest first)
        versions = versions.OrderByDescending(v => v.Version, new VersionComparer()).ToList();

        return Task.FromResult<IReadOnlyList<DownloadedFrpcVersion>>(versions);
    }

    public Task<bool> DeleteVersionAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                logger.LogWarning("Version folder does not exist: {FolderPath}", folderPath);
                return Task.FromResult(false);
            }

            Directory.Delete(folderPath, true);
            logger.LogInformation("Deleted version folder: {FolderPath}", folderPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting version folder: {FolderPath}", folderPath);
            return Task.FromResult(false);
        }
    }

    public string GetVersionFolderName(string version, string platform, string architecture)
    {
        return $"{version}_{platform}_{architecture}";
    }

    public string GetBinaryPathForVersion(string version, string platform, string architecture)
    {
        var folderName = GetVersionFolderName(version, platform, architecture);
        var exeName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
        return Path.Combine(GetDefaultDeploymentDirectory(), folderName, exeName);
    }

    private async Task<string> ExtractTarGzAsync(string archivePath, string targetDirectory,
        CancellationToken cancellationToken)
    {
        // For .tar.gz files, we need to extract them
        var tempDir = Path.Combine(Path.GetTempPath(), "frapa_extract_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDir);

            // Use system tar command if available
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xzf \"{archivePath}\" -C \"{tempDir}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Failed to extract archive: {error}");
                }
            }
            else
            {
                // For Windows, we'd need a library like SharpZipLib
                // For now, just copy the file if it's not actually compressed
                File.Copy(archivePath, Path.Combine(targetDirectory, Path.GetFileName(archivePath)), true);
                return Path.Combine(targetDirectory, Path.GetFileName(archivePath));
            }

            // Find the frpc binary in the extracted files
            var frpcName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
            var foundFiles = Directory.GetFiles(tempDir, frpcName, SearchOption.AllDirectories);

            if (foundFiles.Length > 0)
            {
                var finalPath = Path.Combine(targetDirectory, frpcName);
                File.Copy(foundFiles[0], finalPath, true);
                await SetExecutablePermissionsAsync(finalPath, cancellationToken);
                return finalPath;
            }

            // If no binary found, look for any executable
            var allFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            var executable = allFiles.FirstOrDefault(f =>
                f.Contains("frpc") && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || f.EndsWith("frpc")));

            if (executable != null)
            {
                var finalPath = Path.Combine(targetDirectory, Path.GetFileName(executable));
                File.Copy(executable, finalPath, true);
                await SetExecutablePermissionsAsync(finalPath, cancellationToken);
                return finalPath;
            }

            throw new InvalidOperationException("Could not find frpc binary in extracted archive");
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                    logger.LogInformation("Cleaned up temp extraction directory: {TempDir}", tempDir);
                }
                catch
                {
                    /* Ignore cleanup errors */
                }
            }
        }
    }

    private async Task<string> ExtractZipAsync(string archivePath, string targetDirectory,
        CancellationToken cancellationToken)
    {
        // For Windows or systems where tar is not available
        var tempDir = Path.Combine(Path.GetTempPath(), "frapa_extract_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDir);

            // Use System.IO.Compression for ZIP files
            System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, tempDir);

            // Find the frpc binary
            var frpcName = "frpc.exe";
            var foundFiles = Directory.GetFiles(tempDir, frpcName, SearchOption.AllDirectories);

            if (foundFiles.Length > 0)
            {
                var finalPath = Path.Combine(targetDirectory, frpcName);
                File.Copy(foundFiles[0], finalPath, true);
                await SetExecutablePermissionsAsync(finalPath, cancellationToken);
                return finalPath;
            }

            // If no binary found, look for any executable
            var allFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            var executable = allFiles.FirstOrDefault(f => f.Contains("frpc"));

            if (executable != null)
            {
                var finalPath = Path.Combine(targetDirectory, Path.GetFileName(executable));
                File.Copy(executable, finalPath, true);
                await SetExecutablePermissionsAsync(finalPath, cancellationToken);
                return finalPath;
            }

            throw new InvalidOperationException("Could not find frpc binary in extracted archive");
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                    logger.LogInformation("Cleaned up temp extraction directory: {TempDir}", tempDir);
                }
                catch
                {
                    /* Ignore cleanup errors */
                }
            }
        }
    }

    private static async Task<string> ComputeSha256ChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);

        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Comparer for semantic versions
    /// </summary>
    private class VersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            if (!Version.TryParse(x, out var vx) || !Version.TryParse(y, out var vy))
            {
                return string.CompareOrdinal(x, y);
            }

            return vx.CompareTo(vy);
        }
    }
}
