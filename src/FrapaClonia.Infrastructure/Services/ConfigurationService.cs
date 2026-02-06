using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing frp client configurations
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    public Task<FrpClientConfig?> LoadConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Loading configuration from {FilePath}", filePath);
        return Task.FromResult<FrpClientConfig?>(new FrpClientConfig());
    }

    public Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Saving configuration to {FilePath}", filePath);
        return Task.CompletedTask;
    }

    public Task<string> ExportToJsonAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult("{}");
    }

    public Task<FrpClientConfig?> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult<FrpClientConfig?>(new FrpClientConfig());
    }

    public string GetDefaultConfigPath()
    {
        // TODO: Implement cross-platform path in Phase 2
        return Path.Combine(GetAppDataDirectory(), "frpc.toml");
    }

    public string GetAppDataDirectory()
    {
        // TODO: Implement cross-platform path in Phase 2
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FrapaClonia");
    }
}
