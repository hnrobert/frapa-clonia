using System.Collections.ObjectModel;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using Microsoft.Extensions.Logging;
using ConfigPreset = FrapaClonia.Shared.Models.ConfigPreset;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for managing configuration presets
/// </summary>
public class PresetService : IPresetService
{
    private readonly ILogger<PresetService> _logger;
    private readonly ITomlSerializer _tomlSerializer;
    private readonly ITomlConfigSerializer _tomlConfigSerializer;
    private readonly ICacheService _cacheService;
    private readonly string _presetsDirectory;

    public ObservableCollection<ConfigPreset> Presets { get; } = [];
    public ConfigPreset? CurrentPreset { get; private set; }

    public event EventHandler<PresetChangedEventArgs>? CurrentPresetChanged;

    public PresetService(
        ILogger<PresetService> logger,
        ITomlSerializer tomlSerializer,
        ITomlConfigSerializer tomlConfigSerializer,
        ICacheService cacheService)
    {
        _logger = logger;
        _tomlSerializer = tomlSerializer;
        _tomlConfigSerializer = tomlConfigSerializer;
        _cacheService = cacheService;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _presetsDirectory = Path.Combine(appData, "FrapaClonia", "presets");
    }

    public string GetPresetsDirectory() => _presetsDirectory;

    public string GetPresetFrpcConfigPath(Guid presetId) =>
        Path.Combine(_presetsDirectory, presetId.ToString("N"), "frpc.toml");

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogDebug("Initializing preset service...");

            // Initialize cache service first
            await _cacheService.InitializeAsync();

            // Ensure presets directory exists
            Directory.CreateDirectory(_presetsDirectory);

            // Load all presets
            await LoadPresetsAsync();

            // If no presets exist, create a default one
            if (Presets.Count == 0)
            {
                _logger.LogDebug("No presets found, creating default preset");
                var defaultPreset = await CreatePresetAsync("Default");
                await _cacheService.SetCurrentPresetAsync(defaultPreset.Id);
                await _cacheService.SaveAsync();
            }

            // Set current preset from cache
            var currentPresetId = _cacheService.CurrentPresetId;
            ConfigPreset? current = null;

            if (currentPresetId != Guid.Empty)
            {
                current = Presets.FirstOrDefault(p => p.Id == currentPresetId);
            }

            // Fallback to first preset if not found
            if (current == null && Presets.Count > 0)
            {
                current = Presets[0];
            }

            CurrentPreset = current;
            if (current != null)
            {
                await _cacheService.SetCurrentPresetAsync(current.Id);
                await _cacheService.SaveAsync();
            }

            _logger.LogDebug("Preset service initialized with {Count} presets, current: {Name}",
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
            _logger.LogDebug("Creating preset: {Name}", name);

            var preset = new ConfigPreset(name)
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            // Save preset to files
            await SavePresetToFileAsync(preset);

            Presets.Add(preset);
            _logger.LogDebug("Created preset: {Name} ({Id})", name, preset.Id);

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

            _logger.LogDebug("Deleting preset: {Name} ({Id})", preset.Name, presetId);

            // Delete preset folder
            var folderPath = GetPresetFolderPath(preset.Id);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
            }

            Presets.Remove(preset);

            // If we deleted the current preset, switch to another
            if (CurrentPreset?.Id == presetId)
            {
                var newCurrent = Presets[0];
                await SwitchPresetAsync(newCurrent.Id);
            }

            _logger.LogDebug("Deleted preset: {Name}", preset.Name);
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

            _logger.LogDebug("Switching to preset: {Name} ({Id})", preset.Name, presetId);

            var previousId = CurrentPreset?.Id ?? Guid.Empty;
            CurrentPreset = preset;

            await _cacheService.SetCurrentPresetAsync(preset.Id);
            await _cacheService.SaveAsync();

            CurrentPresetChanged?.Invoke(this, new PresetChangedEventArgs
            {
                PreviousPresetId = previousId,
                CurrentPresetId = presetId,
                CurrentPreset = preset
            });

            _logger.LogDebug("Switched to preset: {Name}", preset.Name);
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

            _logger.LogDebug("Duplicating preset: {Name} ({Id})", preset.Name, presetId);

            var clone = preset.Clone();
            await SavePresetToFileAsync(clone);

            Presets.Add(clone);
            _logger.LogDebug("Duplicated preset: {Name} -> {CloneName}", preset.Name, clone.Name);

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

            _logger.LogDebug("Renaming preset: {OldName} -> {NewName}", preset.Name, newName);

            preset.Name = newName;
            preset.ModifiedAt = DateTime.Now;

            await SavePresetToFileAsync(preset);

            // Raise event to notify UI of name change
            if (CurrentPreset?.Id == presetId)
            {
                await _cacheService.SetCurrentPresetAsync(presetId);
                await _cacheService.SaveAsync();

                CurrentPresetChanged?.Invoke(this, new PresetChangedEventArgs
                {
                    PreviousPresetId = presetId,
                    CurrentPresetId = presetId,
                    CurrentPreset = preset
                });
            }

            _logger.LogDebug("Renamed preset to: {NewName}", newName);
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

            _logger.LogDebug("Exporting preset: {Name} to {Path} as {Format}",
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

            _logger.LogDebug("Exported preset to: {Path}", filePath);
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
            _logger.LogDebug("Importing preset from: {Path} as {Format}", filePath, format);

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

            _logger.LogDebug("Imported preset: {Name}", preset.Name);
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
            _logger.LogDebug("Saved current preset: {Name}", CurrentPreset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving current preset");
            throw;
        }
    }

    #region File Path Methods

    private string GetPresetFolderPath(Guid presetId) =>
        Path.Combine(_presetsDirectory, presetId.ToString("N"));

    private string GetPresetConfigPath(Guid presetId) =>
        Path.Combine(GetPresetFolderPath(presetId), "config.toml");

    private string GetPresetFrpcPath(Guid presetId) =>
        Path.Combine(GetPresetFolderPath(presetId), "frpc.toml");

    #endregion

    #region Loading and Saving

    private async Task LoadPresetsAsync()
    {
        try
        {
            Presets.Clear();

            if (!Directory.Exists(_presetsDirectory))
            {
                return;
            }

            // Scan for preset folders (contain config.toml)
            foreach (var folder in Directory.GetDirectories(_presetsDirectory))
            {
                try
                {
                    var configPath = Path.Combine(folder, "config.toml");
                    var frpcPath = Path.Combine(folder, "frpc.toml");

                    if (!File.Exists(configPath))
                    {
                        continue;
                    }

                    var presetConfig = await _tomlConfigSerializer.DeserializeFromFileAsync<PresetConfig>(configPath);
                    if (presetConfig == null)
                    {
                        _logger.LogWarning("Failed to load preset config from: {Path}", configPath);
                        continue;
                    }

                    var frpcConfig = await _tomlSerializer.DeserializeFromFileAsync(frpcPath);

                    var preset = new ConfigPreset(presetConfig.Preset.Name)
                    {
                        Id = presetConfig.Preset.Id,
                        CreatedAt = presetConfig.Preset.CreatedAt,
                        ModifiedAt = presetConfig.Preset.ModifiedAt,
                        Configuration = frpcConfig ?? new FrpClientConfig(),
                        Deployment = presetConfig.Deployment
                    };

                    Presets.Add(preset);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load preset from folder: {Folder}", folder);
                }
            }

            _logger.LogDebug("Loaded {Count} presets", Presets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading presets");
        }
    }

    private async Task SavePresetToFileAsync(ConfigPreset preset)
    {
        var folderPath = GetPresetFolderPath(preset.Id);
        Directory.CreateDirectory(folderPath);

        // Save config.toml (metadata + deployment settings)
        var presetConfig = new PresetConfig
        {
            Preset = new PresetMetadata
            {
                Id = preset.Id,
                Name = preset.Name,
                CreatedAt = preset.CreatedAt,
                ModifiedAt = preset.ModifiedAt
            },
            Deployment = preset.Deployment
        };

        var configPath = GetPresetConfigPath(preset.Id);
        await _tomlConfigSerializer.SerializeToFileAsync(configPath, presetConfig);

        // Save frpc.toml (FrpClientConfig)
        var frpcPath = GetPresetFrpcPath(preset.Id);
        await _tomlSerializer.SerializeToFileAsync(frpcPath, preset.Configuration);

        _logger.LogDebug("Saved preset '{Name}' to {FolderPath}", preset.Name, folderPath);
    }

    #endregion

    #region INI Export/Import

    private static async Task ExportAsIniAsync(string filePath, FrpClientConfig config)
    {
        var lines = new List<string>
        {
            // Common section
            "[common]"
        };

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

    #endregion
}
