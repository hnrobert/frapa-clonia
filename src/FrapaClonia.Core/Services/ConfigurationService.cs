using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for managing frp client configurations
/// </summary>
public class ConfigurationService(ILogger<ConfigurationService> logger, ITomlSerializer tomlSerializer)
    : IConfigurationService
{
    public Task<FrpClientConfig?> LoadConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Loading configuration from {FilePath}", filePath);
            return tomlSerializer.DeserializeFromFileAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading configuration from {FilePath}", filePath);
            return Task.FromResult<FrpClientConfig?>(null);
        }
    }

    public Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Saving configuration to {FilePath}", filePath);
            return tomlSerializer.SerializeToFileAsync(filePath, configuration, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving configuration to {FilePath}", filePath);
            return Task.CompletedTask;
        }
    }

    public string GetDefaultConfigPath()
    {
        return Path.Combine(GetAppDataDirectory(), "frpc.toml");
    }

    public string GetAppDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia");
    }
}
