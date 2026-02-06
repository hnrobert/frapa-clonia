using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for native executable deployment of frpc
/// </summary>
public class NativeDeploymentService : INativeDeploymentService
{
    private readonly ILogger<NativeDeploymentService> _logger;

    public NativeDeploymentService(ILogger<NativeDeploymentService> logger)
    {
        _logger = logger;
    }

    public async Task<string> DeployFromArchiveAsync(string archivePath, string targetDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deploying frpc from archive {ArchivePath} to {TargetDirectory}", archivePath, targetDirectory);

            // Ensure target directory exists
            Directory.CreateDirectory(targetDirectory);

            // Extract the archive
            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                return await ExtractTarGzAsync(archivePath, targetDirectory, cancellationToken);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return await ExtractZipAsync(archivePath, targetDirectory, cancellationToken);
            }
            else
            {
                // Assume it's already an uncompressed binary
                var fileName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
                var destPath = Path.Combine(targetDirectory, fileName);
                File.Copy(archivePath, destPath, true);
                await SetExecutablePermissionsAsync(destPath, cancellationToken);
                return destPath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying frpc from archive {ArchivePath}", archivePath);
            throw;
        }
    }

    public async Task<bool> VerifyBinaryAsync(string binaryPath, string? expectedChecksum = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(binaryPath))
            {
                _logger.LogWarning("Binary file does not exist: {BinaryPath}", binaryPath);
                return false;
            }

            _logger.LogInformation("Verifying frpc binary at {BinaryPath}", binaryPath);

            // If checksum provided, verify it
            if (!string.IsNullOrEmpty(expectedChecksum))
            {
                var actualChecksum = await ComputeSha256ChecksumAsync(binaryPath, cancellationToken);
                var matches = actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);

                if (!matches)
                {
                    _logger.LogWarning("Checksum mismatch for {BinaryPath}. Expected: {Expected}, Actual: {Actual}",
                        binaryPath, expectedChecksum, actualChecksum);
                }

                return matches;
            }

            // Verify the binary is executable by checking if we can get file info
            var fileInfo = new FileInfo(binaryPath);
            if (fileInfo.Length == 0)
            {
                _logger.LogWarning("Binary file is empty: {BinaryPath}", binaryPath);
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
                    _logger.LogWarning("Could not execute {BinaryPath} to verify", binaryPath);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying binary at {BinaryPath}", binaryPath);
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
        var binaryPath = await GetDeployedBinaryPathAsync(cancellationToken);
        return !string.IsNullOrEmpty(binaryPath) && File.Exists(binaryPath);
    }

    public async Task<string?> GetDeployedBinaryPathAsync(CancellationToken cancellationToken = default)
    {
        var binDir = GetDefaultDeploymentDirectory();
        var exeName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
        var binaryPath = Path.Combine(binDir, exeName);

        if (File.Exists(binaryPath))
        {
            return binaryPath;
        }

        return null;
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

            _logger.LogInformation("Setting executable permissions on {BinaryPath}", binaryPath);

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
                _logger.LogWarning("chmod failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, process.StandardError.ReadToEnd());
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting executable permissions on {BinaryPath}", binaryPath);
            return Task.CompletedTask;
        }
    }

    private async Task<string> ExtractTarGzAsync(string archivePath, string targetDirectory, CancellationToken cancellationToken)
    {
        // For .tar.gz files, we need to extract them
        // This is a simple implementation - for production, use a proper TAR library
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
                try { Directory.Delete(tempDir, true); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    private async Task<string> ExtractZipAsync(string archivePath, string targetDirectory, CancellationToken cancellationToken)
    {
        // For Windows or systems where tar is not available, just copy the file
        // In production, use a proper ZIP extraction library
        var fileName = Path.GetFileNameWithoutExtension(archivePath);
        var destPath = Path.Combine(targetDirectory, fileName + ".exe");

        File.Copy(archivePath, destPath, true);
        await SetExecutablePermissionsAsync(destPath, cancellationToken);
        return destPath;
    }

    private async Task<string> ComputeSha256ChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
