using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FrapaClonia.Infrastructure.Services;

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

    public Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default)
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

    public Task<string> ExportToJsonAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, FrpClientConfigContext.Default.FrpClientConfig);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting configuration to JSON");
            return Task.FromResult("{}");
        }
    }

    public Task<FrpClientConfig?> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = JsonSerializer.Deserialize(json, FrpClientConfigContext.Default.FrpClientConfig);
            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing configuration from JSON");
            return Task.FromResult<FrpClientConfig?>(null);
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
