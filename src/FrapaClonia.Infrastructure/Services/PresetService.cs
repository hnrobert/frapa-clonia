using System.Collections.ObjectModel;
using System.Text.Json;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing configuration presets
/// </summary>
public class PresetService : IPresetService
{
    private readonly ILogger<PresetService> _logger;
    private readonly ITomlSerializer _tomlSerializer;
    private readonly string _presetsDirectory;
    private readonly string _settingsFilePath;
    private Guid _currentPresetId;

    public ObservableCollection<ConfigPreset> Presets { get; } = [];
    public ConfigPreset? CurrentPreset { get; private set; }

    public event EventHandler<PresetChangedEventArgs>? CurrentPresetChanged;

    public PresetService(ILogger<PresetService> logger, ITomlSerializer tomlSerializer)
    {
        _logger = logger;
        _tomlSerializer = tomlSerializer;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "FrapaClonia");
        _presetsDirectory = Path.Combine(baseDir, "presets");
        _settingsFilePath = Path.Combine(baseDir, "preset-settings.json");
    }

    public string GetPresetsDirectory() => _presetsDirectory;

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing preset service...");

            // Ensure presets directory exists
            Directory.CreateDirectory(_presetsDirectory);

            // Load current preset ID from settings
            _currentPresetId = await LoadCurrentPresetIdAsync();

            // Load all presets
            await LoadPresetsAsync();

            // If no presets exist, create a default one
            if (Presets.Count == 0)
            {
                _logger.LogInformation("No presets found, creating default preset");
                var defaultPreset = await CreatePresetAsync("Default");
                _currentPresetId = defaultPreset.Id;
                await SaveCurrentPresetIdAsync();
            }

            // Set current preset
            var current = Presets.FirstOrDefault(p => p.Id == _currentPresetId);
            if (current == null && Presets.Count > 0)
            {
                current = Presets[0];
                _currentPresetId = current.Id;
                await SaveCurrentPresetIdAsync();
            }

            CurrentPreset = current;
            _logger.LogInformation("Preset service initialized with {Count} presets, current: {Name}",
                Presets.Count, CurrentPreset?.Name ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing preset service");
            throw;
        }
    }

    public async Task<ConfigPreset> CreatePresetAsync(string name)
    {
        try
        {
            _logger.LogInformation("Creating preset: {Name}", name);

            var preset = new ConfigPreset(name)
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            // Save preset to file
            await SavePresetToFileAsync(preset);

            Presets.Add(preset);
            _logger.LogInformation("Created preset: {Name} ({Id})", name, preset.Id);

            return preset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating preset: {Name}", name);
            throw;
        }
    }

    public async Task DeletePresetAsync(Guid presetId)
    {
        try
        {
            if (Presets.Count <= 1)
            {
                _logger.LogWarning("Cannot delete the last preset");
                throw new InvalidOperationException("Cannot delete the last preset");
            }

            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
            {
                _logger.LogWarning("Preset not found: {Id}", presetId);
                return;
            }

            _logger.LogInformation("Deleting preset: {Name} ({Id})", preset.Name, presetId);

            // Delete preset file
            var filePath = GetPresetFilePath(presetId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            Presets.Remove(preset);

            // If we deleted the current preset, switch to another
            if (CurrentPreset?.Id == presetId)
            {
                var newCurrent = Presets[0];
                await SwitchPresetAsync(newCurrent.Id);
            }

            _logger.LogInformation("Deleted preset: {Name}", preset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preset: {Id}", presetId);
            throw;
        }
    }

    public async Task SwitchPresetAsync(Guid presetId)
    {
        try
        {
            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
            {
                _logger.LogWarning("Preset not found: {Id}", presetId);
                return;
            }

            _logger.LogInformation("Switching to preset: {Name} ({Id})", preset.Name, presetId);

            var previousId = _currentPresetId;
            _currentPresetId = presetId;
            CurrentPreset = preset;

            await SaveCurrentPresetIdAsync();

            CurrentPresetChanged?.Invoke(this, new PresetChangedEventArgs
            {
                PreviousPresetId = previousId,
                CurrentPresetId = presetId,
                CurrentPreset = preset
            });

            _logger.LogInformation("Switched to preset: {Name}", preset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching to preset: {Id}", presetId);
            throw;
        }
    }

    public async Task<ConfigPreset> DuplicatePresetAsync(Guid presetId)
    {
        try
        {
            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
            {
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            _logger.LogInformation("Duplicating preset: {Name} ({Id})", preset.Name, presetId);

            var clone = preset.Clone();
            await SavePresetToFileAsync(clone);

            Presets.Add(clone);
            _logger.LogInformation("Duplicated preset: {Name} -> {CloneName}", preset.Name, clone.Name);

            return clone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating preset: {Id}", presetId);
            throw;
        }
    }

    public async Task RenamePresetAsync(Guid presetId, string newName)
    {
        try
        {
            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
            {
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            _logger.LogInformation("Renaming preset: {OldName} -> {NewName}", preset.Name, newName);

            preset.Name = newName;
            preset.ModifiedAt = DateTime.Now;

            await SavePresetToFileAsync(preset);

            // Raise event to notify UI of name change
            if (CurrentPreset?.Id == presetId)
            {
                CurrentPresetChanged?.Invoke(this, new PresetChangedEventArgs
                {
                    PreviousPresetId = presetId,
                    CurrentPresetId = presetId,
                    CurrentPreset = preset
                });
            }

            _logger.LogInformation("Renamed preset to: {NewName}", newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming preset: {Id}", presetId);
            throw;
        }
    }

    public async Task ExportPresetAsync(Guid presetId, string filePath, ExportFormat format)
    {
        try
        {
            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
            {
                throw new InvalidOperationException($"Preset not found: {presetId}");
            }

            _logger.LogInformation("Exporting preset: {Name} to {Path} as {Format}",
                preset.Name, filePath, format);

            if (format == ExportFormat.Toml)
            {
                await _tomlSerializer.SerializeToFileAsync(filePath, preset.Configuration);
            }
            else
            {
                // INI format - convert TOML to INI-like format
                await ExportAsIniAsync(filePath, preset.Configuration);
            }

            _logger.LogInformation("Exported preset to: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting preset: {Id}", presetId);
            throw;
        }
    }

    public async Task<ConfigPreset> ImportPresetAsync(string filePath, ExportFormat format)
    {
        try
        {
            _logger.LogInformation("Importing preset from: {Path} as {Format}", filePath, format);

            FrpClientConfig? config;

            if (format == ExportFormat.Toml)
            {
                config = await _tomlSerializer.DeserializeFromFileAsync(filePath);
            }
            else
            {
                // INI format
                config = await ImportFromIniAsync(filePath);
            }

            if (config == null)
            {
                throw new InvalidOperationException("Failed to import configuration from file");
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var preset = new ConfigPreset(fileName)
            {
                Id = Guid.NewGuid(),
                Configuration = config,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            await SavePresetToFileAsync(preset);
            Presets.Add(preset);

            _logger.LogInformation("Imported preset: {Name}", preset.Name);
            return preset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing preset from: {Path}", filePath);
            throw;
        }
    }

    public async Task SaveCurrentPresetAsync()
    {
        if (CurrentPreset == null)
        {
            _logger.LogWarning("No current preset to save");
            return;
        }

        try
        {
            CurrentPreset.ModifiedAt = DateTime.Now;
            await SavePresetToFileAsync(CurrentPreset);
            _logger.LogInformation("Saved current preset: {Name}", CurrentPreset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving current preset");
            throw;
        }
    }

    private string GetPresetFilePath(Guid presetId)
    {
        return Path.Combine(_presetsDirectory, $"{presetId}.json");
    }

    private async Task LoadPresetsAsync()
    {
        try
        {
            Presets.Clear();

            if (!Directory.Exists(_presetsDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(_presetsDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var preset = JsonSerializer.Deserialize(json, PresetSerializationContext.Default.ConfigPreset);
                    if (preset != null)
                    {
                        Presets.Add(preset);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load preset from file: {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} presets", Presets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading presets");
        }
    }

    private async Task SavePresetToFileAsync(ConfigPreset preset)
    {
        var filePath = GetPresetFilePath(preset.Id);
        var json = JsonSerializer.Serialize(preset, PresetSerializationContext.Default.ConfigPreset);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task<Guid> LoadCurrentPresetIdAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return Guid.Empty;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize(json, PresetSettingsContext.Default.PresetSettings);
            return settings?.CurrentPresetId ?? Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load current preset ID from settings");
            return Guid.Empty;
        }
    }

    private async Task SaveCurrentPresetIdAsync()
    {
        try
        {
            var settings = new PresetSettings { CurrentPresetId = _currentPresetId };
            var json = JsonSerializer.Serialize(settings, PresetSettingsContext.Default.PresetSettings);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current preset ID to settings");
        }
    }

    private async Task ExportAsIniAsync(string filePath, FrpClientConfig config)
    {
        var lines = new List<string>();

        // Common section
        lines.Add("[common]");
        if (config.CommonConfig != null)
        {
            var cc = config.CommonConfig;
            if (!string.IsNullOrEmpty(cc.ServerAddr))
                lines.Add($"server_addr = {cc.ServerAddr}");
            lines.Add($"server_port = {cc.ServerPort}");
            if (!string.IsNullOrEmpty(cc.User))
                lines.Add($"user = {cc.User}");

            if (cc.Auth != null)
            {
                lines.Add($"auth_method = {cc.Auth.Method}");
                if (!string.IsNullOrEmpty(cc.Auth.Token))
                    lines.Add($"token = {cc.Auth.Token}");
            }

            if (cc.Transport != null)
            {
                lines.Add($"protocol = {cc.Transport.Protocol}");
                lines.Add($"tls_enable = {cc.Transport.Tls?.Enable ?? true}");
            }
        }

        // Proxies
        foreach (var proxy in config.Proxies)
        {
            lines.Add("");
            lines.Add($"[{proxy.Name}]");
            lines.Add($"type = {proxy.Type}");
            lines.Add($"local_ip = {proxy.LocalIP}");
            lines.Add($"local_port = {proxy.LocalPort}");

            if (proxy.RemotePort.HasValue)
                lines.Add($"remote_port = {proxy.RemotePort}");

            if (!string.IsNullOrEmpty(proxy.SecretKey))
                lines.Add($"sk = {proxy.SecretKey}");
        }

        // Visitors
        foreach (var visitor in config.Visitors)
        {
            lines.Add("");
            lines.Add($"[{visitor.Name}]");
            lines.Add($"type = {visitor.Type}");
            lines.Add($"server_name = {visitor.ServerName}");
            lines.Add($"sk = {visitor.SecretKey}");
            lines.Add($"bind_addr = {visitor.BindAddr}");
            lines.Add($"bind_port = {visitor.BindPort}");
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

    private Task<FrpClientConfig?> ImportFromIniAsync(string filePath)
    {
        // For simplicity, we'll use the TOML serializer for INI files as well
        // since INI and TOML are similar enough for this use case
        return _tomlSerializer.DeserializeFromFileAsync(filePath);
    }
}

/// <summary>
/// Settings for preset persistence
/// </summary>
public class PresetSettings
{
    public Guid CurrentPresetId { get; set; }
}
