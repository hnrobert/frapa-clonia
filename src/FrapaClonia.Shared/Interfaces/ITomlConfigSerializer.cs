namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Generic TOML serializer for simple configuration models
/// Uses Nett library for serialization (different from ITomlSerializer which is specific to FrpClientConfig)
/// </summary>
public interface ITomlConfigSerializer
{
    /// <summary>
    /// Deserializes a TOML file to a configuration object
    /// </summary>
    Task<T?> DeserializeFromFileAsync<T>(string filePath) where T : class, new();

    /// <summary>
    /// Serializes a configuration object to a TOML file
    /// </summary>
    Task SerializeToFileAsync<T>(string filePath, T obj) where T : class;
}
