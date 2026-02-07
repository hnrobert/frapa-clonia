using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Cross-platform process management service
/// </summary>
public class ProcessManager(ILogger<ProcessManager> logger) : IProcessManager
{
    private readonly Dictionary<int, ProcessOutputSubject> _processOutputs = new();

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
            });

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
        if (_processOutputs.TryGetValue(processId, out var subject))
        {
            return subject;
        }

        // Return an empty observable if process not found
        return System.Reactive.Linq.Observable.Empty<string>();
    }

    public Task<bool> IsPortAvailableAsync(int port, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Checking if port {Port} is available", port);

            // Try to bind to the port
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            // If we got here, the port is available
            listener.Stop();
            return Task.FromResult(true);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            logger.LogDebug("Port {Port} is already in use", port);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking port {Port}", port);
            return Task.FromResult(false);
        }
    }

    public Task<int?> GetAvailablePortAsync(int minPort, int maxPort, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding available port between {MinPort} and {MaxPort}", minPort, maxPort);

        // Common ports that might be in use
        var commonPorts = new HashSet<int>
        {
            80, 443, 22, 21, 23, 25, 53, 110, 143, 3306,
            3389, 5432, 6379, 7000, 7001, 8000, 8080, 8888
        };

        // Try to find an available port
        for (int port = minPort; port <= maxPort; port++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip common ports to avoid conflicts
            if (commonPorts.Contains(port))
                continue;

            if (IsPortAvailable(port))
            {
                logger.LogDebug("Found available port: {Port}", port);
                return Task.FromResult<int?>(port);
            }
        }

        logger.LogWarning("No available port found between {MinPort} and {MaxPort}", minPort, maxPort);
        return Task.FromResult<int?>(null);
    }

    private static bool IsPortAvailable(int port)
    {
        // Check TCP
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
        var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();

        var isInUse = tcpConnections.Any(c => c.LocalEndPoint.Port == port)
            || tcpListeners.Any(l => l.Port == port);

        if (isInUse)
            return false;

        // Try to bind to be sure
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Subject for process output that implements IObservable
    /// </summary>
    private class ProcessOutputSubject : IObservable<string>, IDisposable
    {
        private readonly System.Diagnostics.Process _process;
        private readonly ILogger<ProcessManager> _logger;
        private readonly List<IObserver<string>> _observers = new();
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
                    var line = await _process.StandardOutput.ReadLineAsync();
                    if (line == null) break;

                    foreach (var observer in _observers.ToList())
                    {
                        observer.OnNext(line);
                    }
                }

                // Read standard error
                while (!_process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    var line = await _process.StandardError.ReadLineAsync();
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
