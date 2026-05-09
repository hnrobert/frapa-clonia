using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for managing application cache data stored in cache.toml
/// </summary>
public class CacheService : ICacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly ITomlConfigSerializer _tomlSerializer;
    private readonly string _cacheFilePath;
    private AppCache _cache = new();

    public Guid CurrentPresetId => _cache.CurrentPreset.Id;

    public string? GitHubToken
    {
        get => _cache.App.GitHubToken;
        set => _cache.App.GitHubToken = value;
    }

    public CacheService(ILogger<CacheService> logger, ITomlConfigSerializer tomlSerializer)
    {
        _logger = logger;
        _tomlSerializer = tomlSerializer;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "FrapaClonia");
        _cacheFilePath = Path.Combine(baseDir, "cache.toml");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing cache service...");

            var cache = await _tomlSerializer.DeserializeFromFileAsync<AppCache>(_cacheFilePath);
            if (cache != null)
            {
                _cache = cache;
                _logger.LogInformation("Cache loaded, current preset: {PresetId}", _cache.CurrentPreset.Id);
            }
            else
            {
                _cache = new AppCache();
                _logger.LogInformation("No existing cache found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing cache service");
            _cache = new AppCache();
        }
    }

    public Task SetCurrentPresetAsync(Guid presetId)
    {
        _cache.CurrentPreset.Id = presetId;
        _logger.LogInformation("Current preset set to: {PresetId}", presetId);
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        try
        {
            await _tomlSerializer.SerializeToFileAsync(_cacheFilePath, _cache);
            _logger.LogInformation("Cache saved to {FilePath}", _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cache");
        }
    }
}
