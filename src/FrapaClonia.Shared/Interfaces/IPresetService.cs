using System.Collections.ObjectModel;
using Models_ConfigPreset = FrapaClonia.Shared.Models.ConfigPreset;

namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Event arguments for preset change events
/// </summary>
public class PresetChangedEventArgs : EventArgs
{
    public Guid PreviousPresetId { get; init; }
    public Guid CurrentPresetId { get; init; }
    public Models_ConfigPreset? CurrentPreset { get; init; }
}

/// <summary>
/// Export format for presets
/// </summary>
public enum ExportFormat
{
    Toml,
    Ini
}

/// <summary>
/// Service for managing configuration presets
/// </summary>
public interface IPresetService
{
    /// <summary>
    /// Collection of all available presets
    /// </summary>
    ObservableCollection<Models_ConfigPreset> Presets { get; }

    /// <summary>
    /// The currently active preset
    /// </summary>
    Models_ConfigPreset? CurrentPreset { get; }

    /// <summary>
    /// Event raised when the current preset changes
    /// </summary>
    event EventHandler<PresetChangedEventArgs>? CurrentPresetChanged;

    /// <summary>
    /// Initialize the preset service, loading presets from storage
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Create a new preset with the specified name
    /// </summary>
    Task<Models_ConfigPreset> CreatePresetAsync(string name);

    /// <summary>
    /// Delete a preset by its ID
    /// </summary>
    Task DeletePresetAsync(Guid presetId);

    /// <summary>
    /// Switch to a different preset
    /// </summary>
    Task SwitchPresetAsync(Guid presetId);

    /// <summary>
    /// Duplicate an existing preset
    /// </summary>
    Task<Models_ConfigPreset> DuplicatePresetAsync(Guid presetId);

    /// <summary>
    /// Rename a preset
    /// </summary>
    Task RenamePresetAsync(Guid presetId, string newName);

    /// <summary>
    /// Export a preset to a file
    /// </summary>
    Task ExportPresetAsync(Guid presetId, string filePath, ExportFormat format);

    /// <summary>
    /// Import a preset from a file
    /// </summary>
    Task<Models_ConfigPreset> ImportPresetAsync(string filePath, ExportFormat format);

    /// <summary>
    /// Save the current preset to storage
    /// </summary>
    Task SaveCurrentPresetAsync();

    /// <summary>
    /// Get the presets directory path
    /// </summary>
    string GetPresetsDirectory();

    /// <summary>
    /// Get the frpc.toml path for a specific preset
    /// </summary>
    string GetPresetFrpcConfigPath(Guid presetId);
}
