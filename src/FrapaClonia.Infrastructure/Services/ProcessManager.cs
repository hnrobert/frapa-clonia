using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Cross-platform process management service
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly Dictionary<int, IObservable<string>> _processOutputs = new();

    public ProcessManager(ILogger<ProcessManager> logger)
    {
        _logger = logger;
    }

    public Task<ProcessHandle?> StartProcessAsync(ProcessStartOptions startInfo, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Starting process {FileName} with arguments {Arguments}", startInfo.FileName, startInfo.Arguments);
        return Task.FromResult<ProcessHandle?>(new ProcessHandle { ProcessId = 1, ProcessName = startInfo.FileName });
    }

    public Task<bool> StopProcessAsync(int processId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Stopping process {ProcessId}", processId);
        return Task.FromResult(true);
    }

    public Task<bool> IsProcessRunningAsync(int processId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        return Task.FromResult(false);
    }

    public IObservable<string> GetProcessOutput(int processId)
    {
        // TODO: Implement in Phase 4 - using delegate-based observable pattern
        return new ProcessOutputObservable();
    }

    public Task<bool> IsPortAvailableAsync(int port, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Checking if port {Port} is available", port);
        return Task.FromResult(true);
    }

    public Task<int?> GetAvailablePortAsync(int minPort, int maxPort, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Finding available port between {MinPort} and {MaxPort}", minPort, maxPort);
        return Task.FromResult<int?>(minPort);
    }
}

/// <summary>
/// Simple observable for process output
/// </summary>
internal class ProcessOutputObservable : IObservable<string>
{
    public IDisposable Subscribe(IObserver<string> observer)
    {
        // TODO: Implement in Phase 4
        return new StubDisposable();
    }
}

/// <summary>
/// Stub disposable
/// </summary>
internal class StubDisposable : IDisposable
{
    public void Dispose() { }
}
