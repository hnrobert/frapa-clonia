namespace FrapaClonia.Shared.Models;

/// <summary>
/// Configuration stored in config.toml for each preset
/// </summary>
public class PresetConfig
{
    /// <summary>
    /// Preset metadata (id, name, timestamps)
    /// </summary>
    public PresetMetadata Preset { get; set; } = new();

    /// <summary>
    /// Deployment settings for this preset
    /// </summary>
    public DeploymentSettings Deployment { get; set; } = new();
}

/// <summary>
/// Metadata for a preset
/// </summary>
public class PresetMetadata
{
    /// <summary>
    /// Unique identifier for this preset
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for this preset
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// When this preset was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this preset was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}
