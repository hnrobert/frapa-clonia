namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing system services (launchd, systemd, Windows services)
/// </summary>
public interface ISystemServiceManager
{
    /// <summary>
    /// Checks if a service is installed
    /// </summary>
    Task<bool> IsServiceInstalledAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a new service
    /// </summary>
    Task<bool> InstallServiceAsync(ServiceConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a service
    /// </summary>
    Task<bool> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a service
    /// </summary>
    Task<bool> StartServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a service
    /// </summary>
    Task<bool> StopServiceAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a service is currently running
    /// </summary>
    Task<bool> IsServiceRunningAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a service
    /// </summary>
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets whether a service should auto-start on boot
    /// </summary>
    Task<bool> SetAutoStartAsync(string serviceName, bool autoStart, ServiceScope scope = ServiceScope.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default service name for frpc
    /// </summary>
    string GetDefaultServiceName();
}

/// <summary>
/// Configuration for a system service
/// </summary>
public class ServiceConfig
{
    /// <summary>
    /// Service name/identifier
    /// </summary>
    public string ServiceName { get; set; } = "frapa-clonia-frpc";

    /// <summary>
    /// Path to the frpc binary
    /// </summary>
    public string BinaryPath { get; set; } = "";

    /// <summary>
    /// Path to the frpc configuration file
    /// </summary>
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Service scope (user or system level)
    /// </summary>
    public ServiceScope Scope { get; set; } = ServiceScope.User;

    /// <summary>
    /// Whether the service should start automatically on boot
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Description of the service
    /// </summary>
    public string Description { get; set; } = "FrapaClonia frpc client service";
}

/// <summary>
/// Service scope (user vs system level)
/// </summary>
public enum ServiceScope
{
    /// <summary>
    /// User-level service (only runs when user is logged in)
    /// </summary>
    User,

    /// <summary>
    /// System-level service (runs even before login)
    /// </summary>
    System
}

/// <summary>
/// Status of a system service
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Whether the service is installed
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Whether the service is currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Whether the service is set to auto-start
    /// </summary>
    public bool IsAutoStartEnabled { get; set; }

    /// <summary>
    /// Current state string (e.g., "running", "stopped", "failed")
    /// </summary>
    public string State { get; set; } = "unknown";

    /// <summary>
    /// Status message or error
    /// </summary>
    public string? Message { get; set; }
}
