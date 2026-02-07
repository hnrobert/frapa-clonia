using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing the frpc process
/// </summary>
public class FrpcProcessService(ILogger<FrpcProcessService> logger, IProcessManager processManager)
    : IFrpcProcessService
{
    private System.Diagnostics.Process? _currentProcess;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _processLock = new(1, 1);

    public bool IsRunning => _currentProcess is { HasExited: false };
    public int? ProcessId => _currentProcess?.Id;

    public event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;
    public event EventHandler<LogLineEventArgs>? LogLineReceived;

    public async Task<bool> StartAsync(string configPath, CancellationToken cancellationToken = default)
    {
        await _processLock.WaitAsync(cancellationToken);

        try
        {
            // Check if already running
            if (IsRunning)
            {
                logger.LogWarning("Frpc is already running");
                return false;
            }

            // Verify config file exists
            if (!File.Exists(configPath))
            {
                logger.LogError("Config file not found: {ConfigPath}", configPath);
                return false;
            }

            // Get the frpc binary path
            var binaryPath = await GetFrpcBinaryPathAsync();
            if (string.IsNullOrEmpty(binaryPath) || !File.Exists(binaryPath))
            {
                logger.LogError("Frpc binary not found at {BinaryPath}", binaryPath ?? "(null)");
                return false;
            }

            logger.LogInformation("Starting frpc with config {ConfigPath}", configPath);

            var startInfo = new ProcessStartOptions
            {
                FileName = binaryPath,
                Arguments = $"-c \"{configPath}\"",
                WorkingDirectory = Path.GetDirectoryName(configPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var handle = await processManager.StartProcessAsync(startInfo, cancellationToken);
            if (handle == null)
            {
                logger.LogError("Failed to start frpc process");
                return false;
            }

            // Attach to the process for monitoring
            _currentProcess = System.Diagnostics.Process.GetProcessById(handle.ProcessId);
            _currentProcess.EnableRaisingEvents = true;
            _currentProcess.Exited += OnProcessExited;

            // Start reading output
            _ = Task.Run(() => ReadProcessOutputAsync(_cts.Token), cancellationToken);

            OnProcessStateChanged(new ProcessStateChangedEventArgs
            {
                IsRunning = true,
                ProcessId = handle.ProcessId
            });

            logger.LogInformation("Frpc started with PID {ProcessId}", handle.ProcessId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting frpc");
            return false;
        }
        finally
        {
            _processLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _processLock.WaitAsync(cancellationToken);

        try
        {
            if (!IsRunning)
            {
                logger.LogWarning("Frpc is not running");
                return;
            }

            logger.LogInformation("Stopping frpc (PID {ProcessId})", _currentProcess!.Id);

            // Cancel the output reading
            await _cts.CancelAsync();

            // Try graceful shutdown first
            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill(entireProcessTree: false);

                // Wait for process to exit
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await _currentProcess.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Frpc did not exit gracefully, forcing termination");
                    _currentProcess.Kill(entireProcessTree: true);
                }
            }

            OnProcessStateChanged(new ProcessStateChangedEventArgs
            {
                IsRunning = false,
                ProcessId = _currentProcess.Id,
                Timestamp = DateTime.UtcNow
            });

            _currentProcess = null;
            logger.LogInformation("Frpc stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping frpc");
        }
        finally
        {
            _processLock.Release();
        }
    }

    public async Task<bool> RestartAsync(string configPath, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Restarting frpc");
        await StopAsync(cancellationToken);

        // Wait a bit for the process to fully stop
        await Task.Delay(500, cancellationToken);

        return await StartAsync(configPath, cancellationToken);
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var processId = _currentProcess?.Id;
        logger.LogInformation("Frpc process (PID {ProcessId}) exited", processId);

        OnProcessStateChanged(new ProcessStateChangedEventArgs
        {
            IsRunning = false,
            ProcessId = processId,
            Timestamp = DateTime.UtcNow
        });
    }

    private Task ReadProcessOutputAsync(CancellationToken cancellationToken)
    {
        if (_currentProcess == null)
            return Task.CompletedTask;

        try
        {
            // Read standard output
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_currentProcess.HasExited && !cancellationToken.IsCancellationRequested)
                    {
                        var line = await _currentProcess.StandardOutput.ReadLineAsync(cancellationToken);
                        if (line != null)
                        {
                            OnLogLineReceived(line, LogLevel.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error reading stdout");
                }
            }, cancellationToken);

            // Read standard error
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_currentProcess.HasExited && !cancellationToken.IsCancellationRequested)
                    {
                        var line = await _currentProcess.StandardError.ReadLineAsync(cancellationToken);
                        if (line == null) continue;
                        // Parse log level from frpc output
                        var logLevel = ParseLogLevel(line);
                        OnLogLineReceived(line, logLevel);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error reading stderr");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ReadProcessOutputAsync");
        }

        return Task.CompletedTask;
    }

    private static LogLevel ParseLogLevel(string logLine)
    {
        var lower = logLine.ToLower();
        if (lower.Contains("error") || lower.Contains("err")) return LogLevel.Error;
        if (lower.Contains("warn") || lower.Contains("warning")) return LogLevel.Warning;
        if (lower.Contains("info")) return LogLevel.Information;
        if (lower.Contains("debug")) return LogLevel.Debug;
        if (lower.Contains("trace")) return LogLevel.Trace;
        return LogLevel.Information;
    }

    private void OnProcessStateChanged(ProcessStateChangedEventArgs args)
    {
        ProcessStateChanged?.Invoke(this, args);
    }

    private void OnLogLineReceived(string line, LogLevel logLevel)
    {
        LogLineReceived?.Invoke(this, new LogLineEventArgs
        {
            LogLine = line,
            LogLevel = logLevel,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static Task<string?> GetFrpcBinaryPathAsync()
    {
        // Try to find the frpc binary
        var binDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FrapaClonia", "bin");
        var exeName = "frpc" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
        var binaryPath = Path.Combine(binDir, exeName);

        if (File.Exists(binaryPath))
        {
            return Task.FromResult<string?>(binaryPath);
        }

        // Try to find frpc in PATH
        try
        {
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                var paths = envPath.Split(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':');
                foreach (var path in paths)
                {
                    var testPath = Path.Combine(path, exeName);
                    if (File.Exists(testPath))
                    {
                        return Task.FromResult<string?>(testPath);
                    }
                }
            }
        }
        catch
        {
            // Ignore PATH search errors
        }

        return Task.FromResult<string?>(null);
    }
}
