using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for detecting and using package managers to install frpc
/// </summary>
public class PackageManagerService(ILogger<PackageManagerService> logger, IProcessManager processManager)
    : IPackageManagerService
{
    public async Task<IReadOnlyList<PackageManagerInfo>> DetectAvailablePackageManagersAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Detecting available package managers");
        var packageManagers = new List<PackageManagerInfo>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            packageManagers.AddRange(await DetectMacOsPackageManagersAsync(cancellationToken));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            packageManagers.AddRange(await DetectWindowsPackageManagersAsync(cancellationToken));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            packageManagers.AddRange(await DetectLinuxPackageManagersAsync(cancellationToken));
        }

        logger.LogInformation("Detected {Count} package managers", packageManagers.Count);
        return packageManagers;
    }

    public async Task<bool> IsPackageManagerInstalledAsync(string packageManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result =
                await processManager.ExecuteAsync("which", packageManager, cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InstallFrpcAsync(string packageManager, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Installing frpc via {PackageManager}", packageManager);

        var installCommand = GetInstallCommand(packageManager);
        if (installCommand == null)
        {
            logger.LogWarning("No install command for package manager: {PackageManager}", packageManager);
            return false;
        }

        try
        {
            var parts = installCommand.Split(' ', 2);
            var executable = parts[0];
            var args = parts.Length > 1 ? parts[1] : "";

            var result = await processManager.ExecuteAsync(executable, args, cancellationToken: cancellationToken);

            if (result.ExitCode == 0)
            {
                logger.LogInformation("Successfully installed frpc via {PackageManager}", packageManager);
                return true;
            }

            logger.LogWarning("Failed to install frpc via {PackageManager}: {Error}",
                packageManager, result.StandardError);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error installing frpc via {PackageManager}", packageManager);
            return false;
        }
    }

    public async Task<string?> GetFrpcBinaryPathAsync(string packageManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try common locations first
            var commonPaths = GetCommonBinaryPaths(packageManager);
            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Use 'which' or 'where' to find the binary
            var whichCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var result = await processManager.ExecuteAsync(whichCommand, "frpc", cancellationToken: cancellationToken);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return result.StandardOutput.Split('\n').FirstOrDefault()?.Trim();
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting frpc binary path for {PackageManager}", packageManager);
            return null;
        }
    }

    public async Task<bool> UninstallFrpcAsync(string packageManager, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uninstalling frpc via {PackageManager}", packageManager);

        var uninstallCommand = GetUninstallCommand(packageManager);
        if (uninstallCommand == null)
        {
            logger.LogWarning("No uninstall command for package manager: {PackageManager}", packageManager);
            return false;
        }

        try
        {
            var parts = uninstallCommand.Split(' ', 2);
            var executable = parts[0];
            var args = parts.Length > 1 ? parts[1] : "";

            var result = await processManager.ExecuteAsync(executable, args, cancellationToken: cancellationToken);

            if (result.ExitCode == 0)
            {
                logger.LogInformation("Successfully uninstalled frpc via {PackageManager}", packageManager);
                return true;
            }

            logger.LogWarning("Failed to uninstall frpc via {PackageManager}: {Error}",
                packageManager, result.StandardError);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uninstalling frpc via {PackageManager}", packageManager);
            return false;
        }
    }

    #region macOS Package Managers

    private async Task<List<PackageManagerInfo>> DetectMacOsPackageManagersAsync(CancellationToken cancellationToken)
    {
        var managers = new List<PackageManagerInfo>();

        // Homebrew
        var brewInstalled = await CheckCommandExistsAsync("brew", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "brew",
            DisplayName = "Homebrew",
            IsInstalled = brewInstalled,
            CanInstallFrpc = brewInstalled,
            InstallCommand =
                "/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"",
            FrpcInstallCommand = "brew install frpc",
            Platform = "macos"
        });

        return managers;
    }

    #endregion

    #region Windows Package Managers

    private async Task<List<PackageManagerInfo>> DetectWindowsPackageManagersAsync(CancellationToken cancellationToken)
    {
        var managers = new List<PackageManagerInfo>();

        // Scoop
        var scoopInstalled = await CheckCommandExistsAsync("scoop", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "scoop",
            DisplayName = "Scoop",
            IsInstalled = scoopInstalled,
            CanInstallFrpc = scoopInstalled, // Note: may need custom manifest
            InstallCommand = "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser; irm get.scoop.sh | iex",
            FrpcInstallCommand = "scoop install frpc",
            Platform = "windows"
        });

        // Chocolatey
        var chocoInstalled = await CheckCommandExistsAsync("choco", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "choco",
            DisplayName = "Chocolatey",
            IsInstalled = chocoInstalled,
            CanInstallFrpc = chocoInstalled, // Note: may need custom package
            InstallCommand =
                "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))",
            FrpcInstallCommand = "choco install frpc -y",
            Platform = "windows"
        });

        // Winget
        var wingetInstalled = await CheckCommandExistsAsync("winget", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "winget",
            DisplayName = "Windows Package Manager (winget)",
            IsInstalled = wingetInstalled,
            CanInstallFrpc = wingetInstalled,
            InstallCommand = "winget is included with Windows 11 and modern Windows 10 versions",
            FrpcInstallCommand = "winget install frpc",
            Platform = "windows"
        });

        return managers;
    }

    #endregion

    #region Linux Package Managers

    private async Task<List<PackageManagerInfo>> DetectLinuxPackageManagersAsync(CancellationToken cancellationToken)
    {
        var managers = new List<PackageManagerInfo>();

        // apt (Debian/Ubuntu)
        var aptInstalled = await CheckCommandExistsAsync("apt-get", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "apt",
            DisplayName = "APT (Debian/Ubuntu)",
            IsInstalled = aptInstalled,
            CanInstallFrpc = false, // frpc not in default repos
            InstallCommand = "sudo apt update && sudo apt install -y apt-transport-https",
            FrpcInstallCommand = null, // Not available, use GitHub download
            Platform = "linux",
            LinuxDistro = "debian"
        });

        // pacman (Arch Linux)
        var pacmanInstalled = await CheckCommandExistsAsync("pacman", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "pacman",
            DisplayName = "Pacman (Arch Linux)",
            IsInstalled = pacmanInstalled,
            CanInstallFrpc = pacmanInstalled, // Available via AUR
            InstallCommand = "pacman is pre-installed on Arch Linux",
            FrpcInstallCommand = "yay -S frpc", // Note: requires AUR helper
            Platform = "linux",
            LinuxDistro = "arch"
        });

        // apk (Alpine)
        var apkInstalled = await CheckCommandExistsAsync("apk", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "apk",
            DisplayName = "APK (Alpine Linux)",
            IsInstalled = apkInstalled,
            CanInstallFrpc = false, // frpc not in default repos
            InstallCommand = "apk is pre-installed on Alpine Linux",
            FrpcInstallCommand = null, // Not available, use GitHub download
            Platform = "linux",
            LinuxDistro = "alpine"
        });

        // dnf (Fedora/RHEL)
        var dnfInstalled = await CheckCommandExistsAsync("dnf", cancellationToken);
        managers.Add(new PackageManagerInfo
        {
            Name = "dnf",
            DisplayName = "DNF (Fedora/RHEL)",
            IsInstalled = dnfInstalled,
            CanInstallFrpc = false, // frpc not in default repos
            InstallCommand = "dnf is pre-installed on Fedora/RHEL",
            FrpcInstallCommand = null, // Not available, use GitHub download
            Platform = "linux",
            LinuxDistro = "fedora"
        });

        return managers;
    }

    #endregion

    #region Helper Methods

    private async Task<bool> CheckCommandExistsAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var whichCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var result = await processManager.ExecuteAsync(whichCommand, command, cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetInstallCommand(string packageManager)
    {
        return packageManager.ToLowerInvariant() switch
        {
            "brew" => "brew install frpc",
            "scoop" => "scoop install frpc",
            "choco" => "choco install frpc -y",
            "winget" => "winget install frpc",
            "pacman" => "yay -S frpc --noconfirm",
            _ => null
        };
    }

    private static string? GetUninstallCommand(string packageManager)
    {
        return packageManager.ToLowerInvariant() switch
        {
            "brew" => "brew uninstall frpc",
            "scoop" => "scoop uninstall frpc",
            "choco" => "choco uninstall frpc -y",
            "winget" => "winget uninstall frpc",
            "pacman" => "yay -R frpc --noconfirm",
            _ => null
        };
    }

    private static string[] GetCommonBinaryPaths(string packageManager)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Return paths in order of specificity based on package manager
            var paths = new List<string>();

            switch (packageManager)
            {
                // Package manager specific paths
                case "scoop":
                {
                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    paths.Add(Path.Combine(userProfile, "scoop", "apps", "frpc", "current", "frpc.exe"));
                    paths.Add(Path.Combine(userProfile, "scoop", "shims", "frpc.exe"));
                    break;
                }
                case "choco":
                    paths.Add(@"C:\ProgramData\chocolatey\bin\frpc.exe");
                    break;
                case "winget":
                    paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "WinGet", "Links", "frpc.exe"));
                    break;
            }

            // Generic fallback paths
            paths.Add(@"C:\Program Files\frpc\frpc.exe");
            paths.Add(@"C:\frpc\frpc.exe");

            return paths.ToArray();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var paths = new List<string>();

            // Package manager specific paths
            if (packageManager == "brew")
            {
                paths.Add("/opt/homebrew/bin/frpc"); // Apple Silicon
                paths.Add("/usr/local/bin/frpc"); // Intel Macs with Homebrew
            }

            // Generic fallback paths
            paths.Add("/usr/local/bin/frpc");
            paths.Add("/usr/bin/frpc");

            return paths.ToArray();
        }

        // Linux
        {
            var paths = new List<string>();

            switch (packageManager)
            {
                // Package manager specific paths
                case "apt":
                case "dnf":
                    paths.Add("/usr/bin/frpc");
                    paths.Add("/usr/local/bin/frpc");
                    break;
                case "pacman":
                case "apk":
                    paths.Add("/usr/bin/frpc");
                    break;
            }

            // Generic fallback paths
            paths.Add("/usr/local/bin/frpc");
            paths.Add("/usr/bin/frpc");
            paths.Add("/opt/frpc/frpc");
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "frpc"));

            return paths.ToArray();
        }
    }

    #endregion
}
