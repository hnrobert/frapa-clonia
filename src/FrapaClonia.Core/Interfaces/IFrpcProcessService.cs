using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing the frpc process
/// </summary>
public interface IFrpcProcessService
{
    /// <summary>
    /// Starts the frpc process with the specified configuration
    /// </summary>
    Task<bool> StartAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the frpc process
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the frpc process
    /// </summary>
    Task<bool> RestartAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the frpc process is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Event raised when the process state changes
    /// </summary>
    event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;

    /// <summary>
    /// Event raised when a new log line is available
    /// </summary>
    event EventHandler<LogLineEventArgs>? LogLineReceived;

    /// <summary>
    /// Gets the process ID if running
    /// </summary>
    int? ProcessId { get; }
}

/// <summary>
/// Event arguments for process state changes
/// </summary>
public class ProcessStateChangedEventArgs : EventArgs
{
    public bool IsRunning { get; init; }
    public int? ProcessId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for log lines
/// </summary>
public class LogLineEventArgs : EventArgs
{
    public string LogLine { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public LogLevel LogLevel { get; init; } = LogLevel.Information;
}
