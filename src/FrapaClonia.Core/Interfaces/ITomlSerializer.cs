using FrapaClonia.Domain.Models;

namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for serializing/deserializing frpc.toml files
/// </summary>
public interface ITomlSerializer
{
    /// <summary>
    /// Deserializes a frpc.toml file to a configuration object
    /// </summary>
    Task<FrpClientConfig?> DeserializeAsync(string tomlContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes a frpc.toml file from a file path
    /// </summary>
    Task<FrpClientConfig?> DeserializeFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a configuration object to TOML format
    /// </summary>
    Task<string> SerializeAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a configuration object to a file
    /// </summary>
    Task SerializeToFileAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default);
}
