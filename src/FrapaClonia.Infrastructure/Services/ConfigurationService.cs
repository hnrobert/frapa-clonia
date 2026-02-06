using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing frp client configurations
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly ITomlSerializer _tomlSerializer;

    public ConfigurationService(ILogger<ConfigurationService> logger, ITomlSerializer tomlSerializer)
    {
        _logger = logger;
        _tomlSerializer = tomlSerializer;
    }

    public Task<FrpClientConfig?> LoadConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading configuration from {FilePath}", filePath);
            return _tomlSerializer.DeserializeFromFileAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {FilePath}", filePath);
            return Task.FromResult<FrpClientConfig?>(null);
        }
    }

    public Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving configuration to {FilePath}", filePath);
            return _tomlSerializer.SerializeToFileAsync(filePath, configuration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {FilePath}", filePath);
            return Task.CompletedTask;
        }
    }

    public Task<string> ExportToJsonAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(configuration, options);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration to JSON");
            return Task.FromResult("{}");
        }
    }

    public Task<FrpClientConfig?> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var config = JsonSerializer.Deserialize<FrpClientConfig>(json, options);
            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration from JSON");
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
