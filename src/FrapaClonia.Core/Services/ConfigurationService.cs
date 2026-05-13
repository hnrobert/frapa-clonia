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
    public Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Saving configuration to {FilePath}", filePath);
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

    public static string GetAppDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia");
    }
}
