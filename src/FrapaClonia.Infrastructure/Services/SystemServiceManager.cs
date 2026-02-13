using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

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

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting service {ServiceName}", serviceName);
        return await _platformManager.StartServiceAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping service {ServiceName}", serviceName);
        return await _platformManager.StopServiceAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        return await _platformManager.IsServiceRunningAsync(serviceName, scope, cancellationToken);
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        return await _platformManager.GetServiceStatusAsync(serviceName, scope, cancellationToken);
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting auto-start for {ServiceName} to {AutoStart}", serviceName, autoStart);
        return await _platformManager.SetAutoStartAsync(serviceName, autoStart, scope, cancellationToken);
    }

    public string GetDefaultServiceName() => "frapa-clonia-frpc";

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
    Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default);
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default);
    Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unsupported platform service manager
/// </summary>
internal class UnsupportedServiceManager : IPlatformServiceManager
{
    public Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> StartServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> StopServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
        => Task.FromResult(new ServiceStatus { IsInstalled = false, State = "unsupported" });
    public Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope, CancellationToken cancellationToken = default) => Task.FromResult(false);
}

/// <summary>
/// macOS service manager using launchd
/// </summary>
internal class MacOsServiceManager(ILogger logger, IProcessManager processManager) : IPlatformServiceManager
{
    public Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var plistPath = GetPlistPath(serviceName, ServiceScope.User);
        return Task.FromResult(File.Exists(plistPath));
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var plistPath = GetPlistPath(config.ServiceName, config.Scope);
            var plistContent = GenerateLaunchdPlist(config);

            Directory.CreateDirectory(Path.GetDirectoryName(plistPath)!);
            await File.WriteAllTextAsync(plistPath, plistContent, cancellationToken);

            // Load the service
            var result = await processManager.ExecuteAsync("launchctl", $"load \"{plistPath}\"", cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install macOS service");
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
                await processManager.ExecuteAsync("launchctl", $"unload \"{userPlist}\"", cancellationToken: cancellationToken);
                File.Delete(userPlist);
            }

            if (File.Exists(systemPlist))
            {
                await processManager.ExecuteAsync("sudo", $"launchctl unload \"{systemPlist}\"", cancellationToken: cancellationToken);
                File.Delete(systemPlist);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall macOS service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("launchctl", $"start {GetServiceLabel(serviceName)}", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("launchctl", $"stop {GetServiceLabel(serviceName)}", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("launchctl", $"list {GetServiceLabel(serviceName)}", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);
        var isRunning = isInstalled && await IsServiceRunningAsync(serviceName, scope, cancellationToken);

        return new ServiceStatus
        {
            IsInstalled = isInstalled,
            IsRunning = isRunning,
            State = isRunning ? "running" : (isInstalled ? "stopped" : "not_installed")
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        // launchd handles auto-start via RunAtLoad and KeepAlive in the plist
        // Would need to regenerate the plist to change this
        return await Task.FromResult(true);
    }

    private static string GetPlistPath(string serviceName, ServiceScope scope)
    {
        var fileName = $"{GetServiceLabel(serviceName)}.plist";
        return scope == ServiceScope.User
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", fileName)
            : $"/Library/LaunchDaemons/{fileName}";
    }

    private static string GetServiceLabel(string serviceName) => $"com.frapaclonia.{serviceName.Replace("-", "")}";

    private static string GenerateLaunchdPlist(ServiceConfig config)
    {
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
                   <{config.AutoStart.ToString().ToLowerInvariant()}/>
                   <key>KeepAlive</key>
                   <true/>
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
/// Windows service manager using sc.exe
/// </summary>
internal class WindowsServiceManager(ILogger logger, IProcessManager processManager) : IPlatformServiceManager
{
    public async Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            // sc.exe requires admin rights for system-level services
            var binPath = $"\"{config.BinaryPath}\" -c \"{config.ConfigPath}\"";
            var startType = config.AutoStart ? "auto" : "demand";

            var result = await processManager.ExecuteAsync("sc",
                $"create \"{config.ServiceName}\" binPath= {binPath} start= {startType} DisplayName= \"{config.Description}\"",
                cancellationToken: cancellationToken);

            if (result.ExitCode != 0)
            {
                logger.LogError("Failed to create Windows service: {Error}", result.StandardError);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install Windows service");
            return false;
        }
    }

    public async Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Stop first
            await StopServiceAsync(serviceName, ServiceScope.System, cancellationToken);

            var result = await processManager.ExecuteAsync("sc", $"delete \"{serviceName}\"", cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall Windows service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("sc", $"start \"{serviceName}\"", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("sc", $"stop \"{serviceName}\"", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var result = await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"", cancellationToken: cancellationToken);
        return result.ExitCode == 0 && result.StandardOutput.Contains("RUNNING");
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);

        if (!isInstalled)
        {
            return new ServiceStatus { IsInstalled = false, State = "not_installed" };
        }

        var result = await processManager.ExecuteAsync("sc", $"query \"{serviceName}\"", cancellationToken: cancellationToken);
        var output = result.StandardOutput;

        var isRunning = output.Contains("RUNNING");
        var state = isRunning ? "running" : (output.Contains("STOPPED") ? "stopped" : "unknown");

        // Check auto-start
        var qcResult = await processManager.ExecuteAsync("sc", $"qc \"{serviceName}\"", cancellationToken: cancellationToken);
        var autoStart = qcResult.StandardOutput.Contains("AUTO_START");

        return new ServiceStatus
        {
            IsInstalled = true,
            IsRunning = isRunning,
            IsAutoStartEnabled = autoStart,
            State = state
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var startType = autoStart ? "auto" : "demand";
        var result = await processManager.ExecuteAsync("sc", $"config \"{serviceName}\" start= {startType}", cancellationToken: cancellationToken);
        return result.ExitCode == 0;
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
            if (config.AutoStart)
            {
                var enableArgs = config.Scope == ServiceScope.User
                    ? $"--user enable {config.ServiceName}"
                    : $"enable {config.ServiceName}";
                var sudoPrefix = config.Scope == ServiceScope.System ? "sudo " : "";
                await processManager.ExecuteAsync($"{sudoPrefix}systemctl", enableArgs, cancellationToken: cancellationToken);
            }

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
                await processManager.ExecuteAsync("systemctl", $"--user disable {serviceName}", cancellationToken: cancellationToken);
                await processManager.ExecuteAsync("systemctl", "--user daemon-reload", cancellationToken: cancellationToken);
                File.Delete(userUnitPath);
            }

            if (File.Exists(systemUnitPath))
            {
                await processManager.ExecuteAsync("sudo", $"systemctl disable {serviceName}", cancellationToken: cancellationToken);
                await processManager.ExecuteAsync("sudo", "systemctl daemon-reload", cancellationToken: cancellationToken);
                File.Delete(systemUnitPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall Linux service");
            return false;
        }
    }

    public async Task<bool> StartServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user start {serviceName}" : $"start {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> StopServiceAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user stop {serviceName}" : $"stop {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var args = scope == ServiceScope.User ? $"--user is-active {serviceName}" : $"is-active {serviceName}";
        var result = await processManager.ExecuteAsync("systemctl", args, cancellationToken: cancellationToken);
        return result.ExitCode == 0 && result.StandardOutput.Trim() == "active";
    }

    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope, CancellationToken cancellationToken = default)
    {
        var isInstalled = await IsServiceInstalledAsync(serviceName, cancellationToken);
        var isRunning = isInstalled && await IsServiceRunningAsync(serviceName, scope, cancellationToken);

        // Check if enabled
        var enabledArgs = scope == ServiceScope.User ? $"--user is-enabled {serviceName}" : $"is-enabled {serviceName}";
        var enabledResult = await processManager.ExecuteAsync("systemctl", enabledArgs, cancellationToken: cancellationToken);
        var isEnabled = enabledResult.ExitCode == 0;

        return new ServiceStatus
        {
            IsInstalled = isInstalled,
            IsRunning = isRunning,
            IsAutoStartEnabled = isEnabled,
            State = isRunning ? "running" : (isInstalled ? "stopped" : "not_installed")
        };
    }

    public async Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope, CancellationToken cancellationToken = default)
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
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "systemd", "user", fileName)
            : $"/etc/systemd/system/{fileName}";
    }

    private static string GenerateSystemdUnit(ServiceConfig config)
    {
        return $""""
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
               """";
    }
}
