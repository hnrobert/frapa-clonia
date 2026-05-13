using System.Runtime.InteropServices;
using FrapaClonia.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Platform-aware system service manager
/// </summary>
public class SystemServiceManager(ILogger<SystemServiceManager> logger, IProcessManager processManager)
    : ISystemServiceManager
{
    private readonly IPlatformServiceManager _platformManager = CreatePlatformManager(logger, processManager);

    // Create platform-specific implementation

    public async Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await _platformManager.IsServiceInstalledAsync(serviceName, cancellationToken);
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Installing service {ServiceName} with scope {Scope}", config.ServiceName, config.Scope);
        return await _platformManager.InstallServiceAsync(config, cancellationToken);
    }

    public async Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uninstalling service {ServiceName}", serviceName);
        return await _platformManager.UninstallServiceAsync(serviceName, cancellationToken);
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting service {ServiceName}", serviceName);
        return await _platformManager.StartServiceAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping service {ServiceName}", serviceName);
        return await _platformManager.StopServiceAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope = ServiceScope.User,
        CancellationToken cancellationToken = default)
    {
        return await _platformManager.IsServiceRunningAsync(serviceName, scope, cancellationToken);
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope = ServiceScope.User,
        CancellationToken cancellationToken = default)
    {
        return await _platformManager.GetServiceStatusAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart,
        ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting auto-start for {ServiceName} to {AutoStart}", serviceName, autoStart);
        return await _platformManager.SetAutoStartAsync(serviceName, autoStart, scope, cancellationToken);
    }

    public string GetServiceNameForPreset(Guid presetId)
    {
        return $"frapa-clonia-frpc-{presetId:N}";
    }

    private static IPlatformServiceManager CreatePlatformManager(ILogger logger, IProcessManager processManager)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOsServiceManager(logger, processManager);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsServiceManager(logger, processManager);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxServiceManager(logger, processManager);
        }

        return new UnsupportedServiceManager();
    }
}

/// <summary>
/// Platform-specific service manager interface
/// </summary>
internal interface IPlatformServiceManager
{
    Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default);
    Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<bool> StartServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default);
    Task<bool> StopServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default);

    Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default);

    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default);

    Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Unsupported platform service manager
/// </summary>
internal class UnsupportedServiceManager : IPlatformServiceManager
{
    public Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> StartServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> StopServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ServiceStatus { IsInstalled = false, State = "unsupported" });

    public Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}

/// <summary>
/// macOS service manager using launchd
/// </summary>
internal class MacOsServiceManager(ILogger logger, IProcessManager processManager) : IPlatformServiceManager
{
    public Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        // Check both user and system scope
        var userPlist = GetPlistPath(serviceName, ServiceScope.User);
        var systemPlist = GetPlistPath(serviceName, ServiceScope.System);
        return Task.FromResult(File.Exists(userPlist) || File.Exists(systemPlist));
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var plistPath = GetPlistPath(config.ServiceName, config.Scope);
            // Don't auto-start on install - set RunAtLoad to false during install
            var plistContent = GenerateLaunchdPlist(config, runAtLoad: false);
            var plistDir = Path.GetDirectoryName(plistPath)!;

            if (config.Scope == ServiceScope.System)
            {
                // System scope requires admin privileges
                // Use osascript to prompt for credentials and run with elevated privileges
                return await InstallSystemServiceAsync(plistPath, plistContent, cancellationToken);
            }

            // User scope doesn't require elevation
            Directory.CreateDirectory(plistDir);
            await File.WriteAllTextAsync(plistPath, plistContent, cancellationToken);

            // Load the service without starting it (use -w to disable RunAtLoad)
            var result = await processManager.ExecuteAsync("launchctl", $"load -w \"{plistPath}\"",
                cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install macOS service");
            return false;
        }
    }

    private async Task<bool> InstallSystemServiceAsync(string plistPath, string plistContent,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a temporary file for the plist content
            var tempPlistPath = Path.Combine(Path.GetTempPath(), $"frapaclonia_service_{Guid.NewGuid():N}.plist");
            await File.WriteAllTextAsync(tempPlistPath, plistContent, cancellationToken);

            try
            {
                var plistDir = Path.GetDirectoryName(plistPath);

                // Combine all commands into a single command so user only authenticates once
                // Use load -w to not auto-start on install
                var combinedCommand =
                    $"mkdir -p '{plistDir}' && cp '{tempPlistPath}' '{plistPath}' && chmod 644 '{plistPath}' && launchctl load -w '{plistPath}'";

                var result = await ExecuteWithAdminPrivilegesAsync(combinedCommand, cancellationToken);
                if (result) return true;
                logger.LogWarning("Install service command failed");
                return false;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempPlistPath))
                {
                    File.Delete(tempPlistPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install system service with elevated privileges");
            return false;
        }
    }

    public async Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try user scope first, then system
            var userPlist = GetPlistPath(serviceName, ServiceScope.User);
            var systemPlist = GetPlistPath(serviceName, ServiceScope.System);

            if (File.Exists(userPlist))
            {
                await processManager.ExecuteAsync("launchctl", $"unload \"{userPlist}\"",
                    cancellationToken: cancellationToken);
                File.Delete(userPlist);
            }

            if (!File.Exists(systemPlist)) return true;
            // System scope requires admin privileges - combine commands to auth only once
            var combinedCommand = $"launchctl unload '{systemPlist}' && rm '{systemPlist}'";
            await ExecuteWithAdminPrivilegesAsync(combinedCommand, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall macOS service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("launchctl", $"start {GetServiceLabel(serviceName)}",
            cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("launchctl", $"stop {GetServiceLabel(serviceName)}",
            cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        // Use "launchctl list" (no label) which outputs lines in "PID Status Label" format
        var result = await processManager.ExecuteAsync("launchctl", "list",
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
            return false;

        var label = GetServiceLabel(serviceName);
        foreach (var line in result.StandardOutput.Split('\n'))
        {
            if (!line.Contains(label)) continue;

            // Format: PID\tStatus\tLabel
            var parts = line.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var pidStr = parts[0];
            return pidStr != "-" && int.TryParse(pidStr, out var pid) && pid > 0;
        }

        return false;
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);
        var isRunning = isInstalled && await IsServiceRunningAsync(serviceName, scope, cancellationToken);

        // Determine the actual scope of the installed service
        var userPlist = GetPlistPath(serviceName, ServiceScope.User);
        var actualScope = File.Exists(userPlist) ? ServiceScope.User : ServiceScope.System;

        return new ServiceStatus
        {
            IsInstalled = isInstalled,
            IsRunning = isRunning,
            State = isRunning ? "running" : isInstalled ? "stopped" : "not_installed",
            Scope = actualScope
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        // launchd handles auto-start via RunAtLoad and KeepAlive in the plist
        // Would need to regenerate the plist to change this
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Executes a command with admin privileges using osascript.
    /// This will show a system authentication dialog to the user.
    /// </summary>
    private async Task<bool> ExecuteWithAdminPrivilegesAsync(string command, CancellationToken cancellationToken)
    {
        // Write the AppleScript to a temporary file to avoid complex escaping issues
        var tempScriptPath = Path.Combine(Path.GetTempPath(), $"admin_script_{Guid.NewGuid():N}.scpt");

        try
        {
            // Escape backslashes and double quotes for AppleScript string
            var escapedCommand = command
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");

            // Create the AppleScript content
            var scriptContent = $"do shell script \"{escapedCommand}\" with administrator privileges";

            // Write the script to a temp file
            await File.WriteAllTextAsync(tempScriptPath, scriptContent, cancellationToken);

            // Execute the script file
            var result = await processManager.ExecuteAsync("osascript", $"\"{tempScriptPath}\"",
                cancellationToken: cancellationToken);

            if (result.ExitCode != 0)
            {
                logger.LogWarning("Admin command failed with exit code {ExitCode}: {Error}",
                    result.ExitCode, result.StandardError);
            }

            return result.ExitCode == 0;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempScriptPath))
            {
                try
                {
                    File.Delete(tempScriptPath);
                }
                catch
                {
                    /* ignore */
                }
            }
        }
    }

    private static string GetPlistPath(string serviceName, ServiceScope scope)
    {
        var fileName = $"{GetServiceLabel(serviceName)}.plist";
        return scope == ServiceScope.User
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents",
                fileName)
            : $"/Library/LaunchDaemons/{fileName}";
    }

    private static string GetServiceLabel(string serviceName) => $"com.frapaclonia.{serviceName.Replace("-", "")}";

    private static string GenerateLaunchdPlist(ServiceConfig config, bool? runAtLoad = null)
    {
        // Use provided runAtLoad or fall back to config.AutoStart
        var shouldRunAtLoad = runAtLoad ?? config.AutoStart;

        return $""""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>{GetServiceLabel(config.ServiceName)}</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>{config.BinaryPath}</string>
                        <string>-c</string>
                        <string>{config.ConfigPath}</string>
                    </array>
                    <key>RunAtLoad</key>
                    <{shouldRunAtLoad.ToString().ToLowerInvariant()}/>
                    <key>KeepAlive</key>
                    <false/>
                    <key>StandardOutPath</key>
                    <string>/tmp/{config.ServiceName}.log</string>
                    <key>StandardErrorPath</key>
                    <string>/tmp/{config.ServiceName}.err</string>
                </dict>
                </plist>
                """";
    }
}

/// <summary>
/// Windows service manager.
/// User scope: Registry Run key for auto-start + direct process management (no admin required).
/// System scope: sc.exe (requires admin).
/// </summary>
internal class WindowsServiceManager(ILogger logger, IProcessManager processManager) : IPlatformServiceManager
{
    private const string RunKeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run";

    private static string GetUserConfigPath(string serviceName)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia", "tasks", $"{serviceName}.json");
    }

    private static async Task<(string BinaryPath, string ConfigPath)?> ReadUserConfigAsync(string serviceName)
    {
        var path = GetUserConfigPath(serviceName);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return (doc.RootElement.GetProperty("BinaryPath").GetString()!,
            doc.RootElement.GetProperty("ConfigPath").GetString()!);
    }

    private static bool IsProcessRunning(string binaryPath)
    {
        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (string.Equals(p.MainModule?.FileName, binaryPath, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch
            {
                // ignored
            }
        }

        return false;
    }

    private static void KillProcess(string binaryPath)
    {
        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (string.Equals(p.MainModule?.FileName, binaryPath, StringComparison.OrdinalIgnoreCase)) p.Kill();
            }
            catch
            {
                // ignored
            }
        }
    }

    public async Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (File.Exists(GetUserConfigPath(serviceName))) return true;
        var r = await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"",
            cancellationToken: cancellationToken);
        return r.ExitCode == 0;
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        return config.Scope == ServiceScope.User
            ? await InstallUserAsync(config, cancellationToken)
            : await InstallSystemServiceAsync(config, cancellationToken);
    }

    private async Task<bool> InstallUserAsync(ServiceConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var cfgPath = GetUserConfigPath(config.ServiceName);
            Directory.CreateDirectory(Path.GetDirectoryName(cfgPath)!);
            // Manual JSON to avoid JsonSerializer AOT warnings — values are file paths (no special chars needed).
            var binEsc = config.BinaryPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var cfgEsc = config.ConfigPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var json = $"{{\"BinaryPath\":\"{binEsc}\",\"ConfigPath\":\"{cfgEsc}\"}}";
            await File.WriteAllTextAsync(cfgPath, json, cancellationToken);

            if (config.AutoStart)
                await SetRunKeyAsync(config.ServiceName, config.BinaryPath, config.ConfigPath, true, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install user task");
            return false;
        }
    }

    private async Task SetRunKeyAsync(string serviceName, string binaryPath, string configPath, bool enable,
        CancellationToken cancellationToken)
    {
        if (enable)
        {
            var value = $"\\\"{binaryPath}\\\" -c \\\"{configPath}\\\"";
            await processManager.ExecuteAsync("reg",
                $"add \"{RunKeyPath}\" /v \"{serviceName}\" /t REG_SZ /d \"{value}\" /f",
                cancellationToken: cancellationToken);
        }
        else
        {
            await processManager.ExecuteAsync("reg",
                $"delete \"{RunKeyPath}\" /v \"{serviceName}\" /f",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<bool> InstallSystemServiceAsync(ServiceConfig config, CancellationToken cancellationToken)
    {
        try
        {
            // sc create requires the entire command line (binary + args) as a single binPath= value.
            // Outer quotes make it one argument; inner \" are literal quotes inside that argument.
            var innerBin = config.BinaryPath.Replace("\"", "\\\"");
            var innerCfg = config.ConfigPath.Replace("\"", "\\\"");
            var binPath = $"\"\\\"{ innerBin}\\\" -c \\\"{innerCfg}\\\"\"";
            var startType = config.AutoStart ? "auto" : "demand";
            var args =
                $"create \"{config.ServiceName}\" binPath= {binPath} start= {startType} DisplayName= \"{config.Description}\"";
            var result = await processManager.ExecuteAsync("sc", args, cancellationToken: cancellationToken);
            switch (result.ExitCode)
            {
                case 0:
                    return true;
                case 5:
                    return await ExecuteElevatedAsync("sc", args, cancellationToken);
                default:
                    logger.LogError("Failed to create Windows service: {Error}", result.StandardError);
                    return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install Windows service");
            return false;
        }
    }

    // Runs sc.exe elevated via UAC (ShellExecute runas). Returns true if exit code is 0.
    private async Task<bool> ExecuteElevatedAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        // Use full path so ShellExecute can find the executable without PATH lookup.
        var fullPath = fileName.Equals("sc", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe")
            : fileName;

        logger.LogInformation("Requesting elevation for: {FileName} {Arguments}", fullPath, arguments);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fullPath,
            Arguments = arguments,
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };
        try
        {
            var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                logger.LogError("Failed to start elevated process");
                return false;
            }
            await process.WaitForExitAsync(cancellationToken);
            logger.LogInformation("Elevated process exited with code {ExitCode}", process.ExitCode);
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            logger.LogWarning("User cancelled the UAC prompt");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Elevated execution failed");
            return false;
        }
    }

    public async Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfgPath = GetUserConfigPath(serviceName);
            if (File.Exists(cfgPath))
            {
                var cfg = await ReadUserConfigAsync(serviceName);
                if (cfg.HasValue) KillProcess(cfg.Value.BinaryPath);
                await SetRunKeyAsync(serviceName, "", "", false, cancellationToken);
                File.Delete(cfgPath);
                return true;
            }

            await StopServiceAsync(serviceName, ServiceScope.System, cancellationToken);
            var result = await processManager.ExecuteAsync("sc", $"delete \"{serviceName}\"",
                cancellationToken: cancellationToken);
            return result.ExitCode switch
            {
                0 => true,
                5 => await ExecuteElevatedAsync("sc", $"delete \"{serviceName}\"", cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall Windows service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        if (scope == ServiceScope.User)
        {
            var cfg = await ReadUserConfigAsync(serviceName);
            if (cfg == null) return false;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cfg.Value.BinaryPath,
                    Arguments = $"-c \"{cfg.Value.ConfigPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start frpc process");
                return false;
            }
        }

        var r = await processManager.ExecuteAsync("sc", $"start \"{serviceName}\"",
            cancellationToken: cancellationToken);
        return r.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        if (scope == ServiceScope.User)
        {
            var cfg = await ReadUserConfigAsync(serviceName);
            if (cfg == null) return false;
            KillProcess(cfg.Value.BinaryPath);
            return true;
        }

        var r = await processManager.ExecuteAsync("sc", $"stop \"{serviceName}\"",
            cancellationToken: cancellationToken);
        return r.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        if (scope == ServiceScope.User)
        {
            var cfg = await ReadUserConfigAsync(serviceName);
            return cfg.HasValue && IsProcessRunning(cfg.Value.BinaryPath);
        }

        var r = await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"",
            cancellationToken: cancellationToken);
        return r.ExitCode == 0 && r.StandardOutput.Contains("RUNNING");
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        if (scope == ServiceScope.User)
        {
            var cfg = await ReadUserConfigAsync(serviceName);
            if (cfg == null) return new ServiceStatus { IsInstalled = false, State = "not_installed" };
            var isRunning = IsProcessRunning(cfg.Value.BinaryPath);
            var regResult = await processManager.ExecuteAsync("reg",
                $"query \"{RunKeyPath}\" /v \"{serviceName}\"", cancellationToken: cancellationToken);
            return new ServiceStatus
            {
                IsInstalled = true, IsRunning = isRunning,
                IsAutoStartEnabled = regResult.ExitCode == 0,
                State = isRunning ? "running" : "stopped"
            };
        }

        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);
        if (!isInstalled) return new ServiceStatus { IsInstalled = false, State = "not_installed" };

        var result =
            await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"", cancellationToken: cancellationToken);
        var output = result.StandardOutput;
        var running = output.Contains("RUNNING");
        var state = running ? "running" : output.Contains("STOPPED") ? "stopped" : "unknown";
        var qc = await processManager.ExecuteAsync("sc", $"qc \"{serviceName}\"", cancellationToken: cancellationToken);
        return new ServiceStatus
        {
            IsInstalled = true, IsRunning = running, IsAutoStartEnabled = qc.StandardOutput.Contains("AUTO_START"),
            State = state
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        if (scope == ServiceScope.User)
        {
            var cfg = await ReadUserConfigAsync(serviceName);
            if (cfg == null) return false;
            await SetRunKeyAsync(serviceName, cfg.Value.BinaryPath, cfg.Value.ConfigPath, autoStart, cancellationToken);
            return true;
        }

        var startType = autoStart ? "auto" : "demand";
        var args = $"config \"{serviceName}\" start= {startType}";
        var scResult = await processManager.ExecuteAsync("sc", args, cancellationToken: cancellationToken);
        if (scResult.ExitCode == 0) return true;
        if (scResult.ExitCode == 5)
            return await ExecuteElevatedAsync("sc", args, cancellationToken);
        return false;
    }
}

/// <summary>
/// Linux service manager using systemd
/// </summary>
internal class LinuxServiceManager(ILogger logger, IProcessManager processManager) : IPlatformServiceManager
{
    public Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var unitPath = GetUnitPath(serviceName, ServiceScope.User);
        return Task.FromResult(File.Exists(unitPath));
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var unitPath = GetUnitPath(config.ServiceName, config.Scope);
            var unitContent = GenerateSystemdUnit(config);

            Directory.CreateDirectory(Path.GetDirectoryName(unitPath)!);
            await File.WriteAllTextAsync(unitPath, unitContent, cancellationToken);

            // Reload systemd
            var reloadArgs = config.Scope == ServiceScope.User ? "--user daemon-reload" : "daemon-reload";
            await processManager.ExecuteAsync("systemctl", reloadArgs, cancellationToken: cancellationToken);

            // Enable if auto-start
            if (!config.AutoStart) return true;
            var enableArgs = config.Scope == ServiceScope.User
                ? $"--user enable {config.ServiceName}"
                : $"enable {config.ServiceName}";
            var sudoPrefix = config.Scope == ServiceScope.System ? "sudo " : "";
            await processManager.ExecuteAsync($"{sudoPrefix}systemctl", enableArgs,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install Linux service");
            return false;
        }
    }

    public async Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try user scope first
            var userUnitPath = GetUnitPath(serviceName, ServiceScope.User);
            var systemUnitPath = GetUnitPath(serviceName, ServiceScope.System);

            if (File.Exists(userUnitPath))
            {
                await processManager.ExecuteAsync("systemctl", $"--user disable {serviceName}",
                    cancellationToken: cancellationToken);
                await processManager.ExecuteAsync("systemctl", "--user daemon-reload",
                    cancellationToken: cancellationToken);
                File.Delete(userUnitPath);
            }

            if (!File.Exists(systemUnitPath)) return true;

            await processManager.ExecuteAsync("sudo", $"systemctl disable {serviceName}",
                cancellationToken: cancellationToken);
            await processManager.ExecuteAsync("sudo", "systemctl daemon-reload", cancellationToken: cancellationToken);
            File.Delete(systemUnitPath);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall Linux service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user start {serviceName}" : $"start {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user stop {serviceName}" : $"stop {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user is-active {serviceName}" : $"is-active {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0 && result.StandardOutput.Trim() == "active";
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);
        var isRunning = isInstalled && await IsServiceRunningAsync(serviceName, scope, cancellationToken);

        // Check if enabled
        var enabledArgs = scope == ServiceScope.User ? $"--user is-enabled {serviceName}" : $"is-enabled {serviceName}";
        var enabledResult =
            await processManager.ExecuteAsync("systemctl", enabledArgs, cancellationToken: cancellationToken);
        var isEnabled = enabledResult.ExitCode == 0;

        return new ServiceStatus
        {
            IsInstalled = isInstalled,
            IsRunning = isRunning,
            IsAutoStartEnabled = isEnabled,
            State = isRunning ? "running" : isInstalled ? "stopped" : "not_installed"
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope,
        CancellationToken cancellationToken = default)
    {
        var action = autoStart ? "enable" : "disable";
        var args = scope == ServiceScope.User ? $"--user {action} {serviceName}" : $"{action} {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    private static string GetUnitPath(string serviceName, ServiceScope scope)
    {
        var fileName = $"{serviceName}.service";
        return scope == ServiceScope.User
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "systemd",
                "user", fileName)
            : $"/etc/systemd/system/{fileName}";
    }

    private static string GenerateSystemdUnit(ServiceConfig config)
    {
        return $"""
                [Unit]
                Description={config.Description}
                After=network.target

                [Service]
                Type=simple
                ExecStart={config.BinaryPath} -c {config.ConfigPath}
                Restart=always
                RestartSec=5

                [Install]
                WantedBy=default.target
                """;
    }
}