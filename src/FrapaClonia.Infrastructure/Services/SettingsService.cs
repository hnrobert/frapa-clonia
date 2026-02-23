using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing application settings stored in settings.toml
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ITomlConfigSerializer _tomlSerializer;
    private readonly string _settingsFilePath;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

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
                _settings = settings;
                _logger.LogInformation("Settings loaded from {FilePath}", _settingsFilePath);
            }
            else
            {
                _settings = new AppSettings();
                _logger.LogInformation("No settings file found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from {FilePath}", _settingsFilePath);
            _settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            await _tomlSerializer.SerializeToFileAsync(_settingsFilePath, _settings);
            _logger.LogInformation("Settings saved to {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to {FilePath}", _settingsFilePath);
        }
    }
}
