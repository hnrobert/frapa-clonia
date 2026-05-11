using System.Runtime.InteropServices;
using FrapaClonia.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Cross-platform process management service
/// </summary>
public class ProcessManager(ILogger<ProcessManager> logger) : IProcessManager
{
    private readonly Dictionary<int, ProcessOutputSubject> _processOutputs = new();

    // Cached shell PATH, resolved once on first use
    private string? _shellPath;
    private Task<string?>? _shellPathTask;

    private Task<string?> GetShellPathAsync()
    {
        return _shellPathTask ??= ResolveShellPathAsync();
    }

    private async Task<string?> ResolveShellPathAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return null;
        }

        try
        {
            var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shell,
                // -i loads ~/.zshrc (interactive), -l loads ~/.zprofile (login)
                // Together they cover all common PATH customizations
                Arguments = "-i -l -c \"echo $PATH\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Take the last non-empty line: interactive mode may print extra output
            _shellPath = output.Split('\n')
                .LastOrDefault(l => l.Contains('/') && !string.IsNullOrWhiteSpace(l))
                ?.Trim();

            if (!string.IsNullOrEmpty(_shellPath))
            {
                logger.LogInformation("Resolved shell PATH ({Shell})", shell);
                logger.LogDebug("Shell PATH: {Path}", _shellPath);
            }
            else
            {
                logger.LogWarning("Shell PATH resolution returned empty output");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve shell PATH");
        }

        return _shellPath;
    }

    private void ApplyShellPath(System.Diagnostics.ProcessStartInfo startInfo)
    {
        if (_shellPath is not null)
        {
            startInfo.Environment["PATH"] = _shellPath;
        }
    }

    public Task<ProcessHandle?> StartProcessAsync(ProcessStartOptions startInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting process: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = startInfo.FileName,
                    Arguments = startInfo.Arguments,
                    WorkingDirectory = startInfo.WorkingDirectory,
                    RedirectStandardOutput = startInfo.RedirectStandardOutput,
                    RedirectStandardError = startInfo.RedirectStandardError,
                    UseShellExecute = startInfo.UseShellExecute
                }
            };

            // Set environment variables if provided
            if (startInfo.EnvironmentVariables != null)
            {
                foreach (var kvp in startInfo.EnvironmentVariables)
                {
                    process.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            ApplyShellPath(process.StartInfo);

            process.Start();

            var handle = new ProcessHandle
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                HasExited = false
            };

            // Create output subject for this process
            _processOutputs[process.Id] = new ProcessOutputSubject(process, logger);

            // Monitor process exit
            _ = Task.Run(() =>
            {
                process.WaitForExit();
                if (_processOutputs.TryGetValue(process.Id, out var subject))
                {
                    subject.OnCompleted();
                }
            }, cancellationToken);

            logger.LogInformation("Process started with PID {ProcessId}", handle.ProcessId);
            return Task.FromResult<ProcessHandle?>(handle);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting process {FileName}", startInfo.FileName);
            return Task.FromResult<ProcessHandle?>(null);
        }
    }

    public Task<bool> StopProcessAsync(int processId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Stopping process {ProcessId}", processId);

            var process = System.Diagnostics.Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);

            // Wait for exit
            process.WaitForExit(5000);

            var stopped = process.HasExited;
            if (stopped)
            {
                logger.LogInformation("Process {ProcessId} stopped successfully", processId);
            }
            else
            {
                logger.LogWarning("Process {ProcessId} did not stop gracefully", processId);
            }

            return Task.FromResult(stopped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error stopping process {ProcessId}", processId);
            return Task.FromResult(false);
        }
    }

    public Task<bool> IsProcessRunningAsync(int processId, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processId);
            var isRunning = !process.HasExited;

            if (!isRunning)
            {
                // Clean up the output subject
                _processOutputs.Remove(processId);
            }

            return Task.FromResult(isRunning);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Process {ProcessId} not found", processId);
            return Task.FromResult(false);
        }
    }

    public IObservable<string> GetProcessOutput(int processId)
    {
        return _processOutputs.TryGetValue(processId, out var subject) ? subject :
            // Return an empty observable if process not found
            System.Reactive.Linq.Observable.Empty<string>();
    }

    public async Task<ProcessResult> ExecuteAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Executing command: {FileName} {Arguments}", fileName, arguments);

            // Ensure shell PATH is resolved for packaged apps
            _ = await GetShellPathAsync();

            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ApplyShellPath(process.StartInfo);

            process.Start();

            var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var result = new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = standardOutput,
                StandardError = standardError
            };

            logger.LogInformation("Command completed with exit code {ExitCode}", process.ExitCode);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command: {FileName} {Arguments}", fileName, arguments);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardOutput = string.Empty,
                StandardError = ex.Message
            };
        }
    }

    /// <summary>
    /// Subject for process output that implements IObservable
    /// </summary>
    private class ProcessOutputSubject : IObservable<string>, IDisposable
    {
        private readonly System.Diagnostics.Process _process;
        private readonly ILogger<ProcessManager> _logger;
        private readonly List<IObserver<string>> _observers = [];
        private readonly CancellationTokenSource _cts = new();

        public ProcessOutputSubject(System.Diagnostics.Process process, ILogger<ProcessManager> logger)
        {
            _process = process;
            _logger = logger;
            _ = Task.Run(() => ReadOutputAsync(_cts.Token));
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber(this, observer);
        }

        public void OnCompleted()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
            _observers.Clear();
        }

        private async Task ReadOutputAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Read standard output
                while (!_process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
                    if (line == null) break;

                    foreach (var observer in _observers.ToList())
                    {
                        observer.OnNext(line);
                    }
                }

                // Read standard error
                while (!_process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    var line = await _process.StandardError.ReadLineAsync(cancellationToken);
                    if (line == null) break;

                    foreach (var observer in _observers.ToList())
                    {
                        observer.OnNext(line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading process output");
            }
            finally
            {
                OnCompleted();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            OnCompleted();
        }

        private class Unsubscriber(ProcessOutputSubject subject, IObserver<string> observer) : IDisposable
        {
            public void Dispose()
            {
                subject._observers.Remove(observer);
            }
        }
    }
}
