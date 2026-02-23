using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Nett;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Generic TOML serializer for simple configuration models using Nett library
/// </summary>
public class TomlConfigSerializer(ILogger<TomlConfigSerializer> logger) : ITomlConfigSerializer
{
    public Task<T?> DeserializeFromFileAsync<T>(string filePath) where T : class, new()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogInformation("TOML config file not found at {FilePath}, returning default", filePath);
                return Task.FromResult<T?>(null);
            }

            var result = Toml.ReadFile<T>(filePath);
            logger.LogInformation("Loaded TOML config from {FilePath}", filePath);
            return Task.FromResult<T?>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading TOML config file at {FilePath}", filePath);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SerializeToFileAsync<T>(string filePath, T obj) where T : class
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Toml.WriteFile(obj, filePath);
            logger.LogInformation("Saved TOML config to {FilePath}", filePath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing TOML config file at {FilePath}", filePath);
            return Task.CompletedTask;
        }
    }
}
