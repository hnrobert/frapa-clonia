using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing the frpc process
/// </summary>
public class FrpcProcessService : IFrpcProcessService
{
    private readonly ILogger<FrpcProcessService> _logger;
    private int? _processId;

    public FrpcProcessService(ILogger<FrpcProcessService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning => _processId.HasValue;
    public int? ProcessId => _processId;

    public event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;
    public event EventHandler<LogLineEventArgs>? LogLineReceived;

    public Task<bool> StartAsync(string configPath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Starting frpc with config {ConfigPath}", configPath);
        return Task.FromResult(true);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Stopping frpc");
        return Task.CompletedTask;
    }

    public Task<bool> RestartAsync(string configPath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 4
        _logger.LogInformation("Restarting frpc");
        return Task.FromResult(true);
    }
}
