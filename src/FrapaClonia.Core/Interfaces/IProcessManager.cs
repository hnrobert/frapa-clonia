namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Cross-platform process management service
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Starts a process with the specified configuration
    /// </summary>
    Task<ProcessHandle?> StartProcessAsync(ProcessStartOptions startInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a process by ID
    /// </summary>
    Task<bool> StopProcessAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a process is running
    /// </summary>
    Task<bool> IsProcessRunningAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets process output lines
    /// </summary>
    IObservable<string> GetProcessOutput(int processId);

    /// <summary>
    /// Checks if a port is available
    /// </summary>
    Task<bool> IsPortAvailableAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an available port in the specified range
    /// </summary>
    Task<int?> GetAvailablePortAsync(int minPort, int maxPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command and returns the result
    /// </summary>
    Task<ProcessResult> ExecuteAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a process execution
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Exit code of the process
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output content
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Standard error content
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Whether the process exited successfully (exit code 0)
    /// </summary>
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Handle to a running process
/// </summary>
public class ProcessHandle
{
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public bool HasExited { get; set; }
    public int? ExitCode { get; set; }
}

/// <summary>
/// Process start options
/// </summary>
public class ProcessStartOptions
{
    public required string FileName { get; init; }
    public required string Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public Dictionary<string, string>? EnvironmentVariables { get; init; }
    public bool RedirectStandardOutput { get; init; }
    public bool RedirectStandardError { get; init; }
    public bool UseShellExecute { get; init; }
}
