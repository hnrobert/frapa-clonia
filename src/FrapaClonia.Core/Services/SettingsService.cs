using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for managing application settings stored in settings.toml
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ITomlConfigSerializer _tomlSerializer;
    private readonly string _settingsFilePath;

    public AppSettings Settings { get; private set; } = new();

    public SettingsService(ILogger<SettingsService> logger, ITomlConfigSerializer tomlSerializer)
    {
        _logger = logger;
        _tomlSerializer = tomlSerializer;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsFilePath = Path.Combine(appData, "FrapaClonia", "settings.toml");
    }

    public async Task LoadAsync()
    {
        try
        {
            var settings = await _tomlSerializer.DeserializeFromFileAsync<AppSettings>(_settingsFilePath);
            if (settings != null)
            {
                Settings = settings;
                _logger.LogDebug("Settings loaded from {FilePath}", _settingsFilePath);
            }
            else
            {
                Settings = new AppSettings();
                _logger.LogDebug("No settings file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from {FilePath}", _settingsFilePath);
            Settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            await _tomlSerializer.SerializeToFileAsync(_settingsFilePath, Settings);
            _logger.LogDebug("Settings saved to {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to {FilePath}", _settingsFilePath);
        }
    }
}
