using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for configuring auto-start on boot
/// </summary>
public class AutoStartService(ILogger<AutoStartService> logger) : IAutoStartService
{
    private const string AppName = "FrapaClonia";

    public bool IsAutoStartSupported =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public Task<bool> IsAutoStartEnabledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isEnabled = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? IsWindowsAutoStartEnabled() :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? IsMacOSAutoStartEnabled() :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IsLinuxAutoStartEnabled();

            return Task.FromResult(isEnabled);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking auto-start status");
            return Task.FromResult(false);
        }
    }

    [UnconditionalSuppressMessage("SingleFile",
        "IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file",
        Justification = "<Pending>")]
    public Task EnableAutoStartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnableWindowsAutoStart();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                EnableMacOSAutoStart();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                EnableLinuxAutoStart();
            }
            else
            {
                logger.LogWarning("Auto-start is not supported on this platform");
            }

            logger.LogInformation("Auto-start enabled successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enabling auto-start");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DisableAutoStartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DisableWindowsAutoStart();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                DisableMacOSAutoStart();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DisableLinuxAutoStart();
            }
            else
            {
                logger.LogWarning("Auto-start is not supported on this platform");
            }

            logger.LogInformation("Auto-start disabled successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disabling auto-start");
            throw;
        }

        return Task.CompletedTask;
    }

    #region Windows

    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [SupportedOSPlatform("windows")]
    private static bool IsWindowsAutoStartEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(WindowsRunKeyPath, false);
        var value = key?.GetValue(AppName);
        return value != null;
    }

    [SupportedOSPlatform("windows")]
    [RequiresAssemblyFiles("Calls GetExecutablePath()")]
    private static void EnableWindowsAutoStart()
    {
        var executablePath = GetExecutablePath();
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new InvalidOperationException("Could not determine executable path");
        }

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(WindowsRunKeyPath, true);
        key?.SetValue(AppName, $"\"{executablePath}\"");
    }

    [SupportedOSPlatform("windows")]
    private static void DisableWindowsAutoStart()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(WindowsRunKeyPath, true);
        key?.DeleteValue(AppName, false);
    }

    #endregion

    #region macOS

    [SupportedOSPlatform("osx")]
    private static string MacOsLaunchAgentDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "LaunchAgents"
    );

    [SupportedOSPlatform("osx")]
    private static string MacOsPlistFileName => $"com.frapaclonia.{AppName.ToLower()}.plist";

    [SupportedOSPlatform("osx")]
    private static bool IsMacOSAutoStartEnabled()
    {
        var plistPath = Path.Combine(MacOsLaunchAgentDir, MacOsPlistFileName);
        return File.Exists(plistPath);
    }

    [SupportedOSPlatform("osx")]
    [RequiresAssemblyFiles("Calls GetExecutablePath()")]
    private static void EnableMacOSAutoStart()
    {
        var executablePath = GetExecutablePath();
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new InvalidOperationException("Could not determine executable path");
        }

        var launchAgentDir = MacOsLaunchAgentDir;
        Directory.CreateDirectory(launchAgentDir);

        var plistPath = Path.Combine(launchAgentDir, MacOsPlistFileName);
        var plistContent = GenerateMacOSPlist(executablePath);

        File.WriteAllText(plistPath, plistContent);
    }

    [SupportedOSPlatform("osx")]
    private static void DisableMacOSAutoStart()
    {
        var plistPath = Path.Combine(MacOsLaunchAgentDir, MacOsPlistFileName);
        if (File.Exists(plistPath))
        {
            File.Delete(plistPath);
        }
    }

    [SupportedOSPlatform("osx")]
    private static string GenerateMacOSPlist(string executablePath)
    {
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
               $"<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
               $"<plist version=\"1.0\">\n" +
               $"<dict>\n" +
               $"    <key>Label</key>\n" +
               $"    <string>com.frapaclonia.{AppName.ToLower()}</string>\n" +
               $"    <key>ProgramArguments</key>\n" +
               $"    <array>\n" +
               $"        <string>{executablePath}</string>\n" +
               $"    </array>\n" +
               $"    <key>RunAtLoad</key>\n" +
               $"    <true/>\n" +
               $"    <key>KeepAlive</key>\n" +
               $"    <false/>\n" +
               $"</dict>\n" +
               $"</plist>\n";
    }

    #endregion

    #region Linux

    [SupportedOSPlatform("linux")]
    private static string LinuxAutoStartDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "autostart"
    );

    [SupportedOSPlatform("linux")] private static string LinuxDesktopFileName => $"{AppName.ToLower()}.desktop";

    [SupportedOSPlatform("linux")]
    private static bool IsLinuxAutoStartEnabled()
    {
        var desktopPath = Path.Combine(LinuxAutoStartDir, LinuxDesktopFileName);
        return File.Exists(desktopPath);
    }

    [SupportedOSPlatform("linux")]
    [RequiresAssemblyFiles("Calls GetExecutablePath()")]
    private static void EnableLinuxAutoStart()
    {
        var executablePath = GetExecutablePath();
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new InvalidOperationException("Could not determine executable path");
        }

        var autoStartDir = LinuxAutoStartDir;
        Directory.CreateDirectory(autoStartDir);

        var desktopPath = Path.Combine(autoStartDir, LinuxDesktopFileName);
        var desktopContent = GenerateLinuxDesktopFile(executablePath);

        File.WriteAllText(desktopPath, desktopContent);
    }

    [SupportedOSPlatform("linux")]
    private static void DisableLinuxAutoStart()
    {
        var desktopPath = Path.Combine(LinuxAutoStartDir, LinuxDesktopFileName);
        if (File.Exists(desktopPath))
        {
            File.Delete(desktopPath);
        }
    }

    [SupportedOSPlatform("linux")]
    private static string GenerateLinuxDesktopFile(string executablePath)
    {
        return "[Desktop Entry]\n" +
               "Type=Application\n" +
               $"Name={AppName}\n" +
               $"Exec={executablePath}\n" +
               "X-GNOME-Autostart-enabled=true\n" +
               "Hidden=false\n" +
               "NoDisplay=false\n" +
               "Comment=FrapaClonia Auto-start\n" +
               "X-GNOME-Autostart-Delay=0\n";
    }

    #endregion

    [RequiresAssemblyFiles("Calls System.Reflection.Assembly.Location")]
    private static string? GetExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
        {
            return processPath;
        }

        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(entryAssembly) && File.Exists(entryAssembly))
        {
            return entryAssembly;
        }

        return null;
    }
}