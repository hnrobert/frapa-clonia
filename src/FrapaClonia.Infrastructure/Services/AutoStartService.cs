using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for configuring auto-start on boot
/// </summary>
public class AutoStartService : IAutoStartService
{
    private readonly ILogger<AutoStartService> _logger;

    public AutoStartService(ILogger<AutoStartService> logger)
    {
        _logger = logger;
    }

    public bool IsAutoStartSupported => true;  // TODO: Implement platform-specific check in Phase 8

    public Task<bool> IsAutoStartEnabledAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 8
        return Task.FromResult(false);
    }

    public Task EnableAutoStartAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 8
        _logger.LogInformation("Enabling auto-start");
        return Task.CompletedTask;
    }

    public Task DisableAutoStartAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 8
        _logger.LogInformation("Disabling auto-start");
        return Task.CompletedTask;
    }
}
