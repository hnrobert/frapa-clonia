using FrapaClonia.Shared.Models;

namespace FrapaClonia.Shared.Interfaces;

/// <summary>
/// Service for managing frp client configurations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Saves configuration to a file
    /// </summary>
    Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default configuration file path
    /// </summary>
    string GetDefaultConfigPath();
}
