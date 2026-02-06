using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing frp client configurations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from a file
    /// </summary>
    Task<FrpClientConfig?> LoadConfigurationAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves configuration to a file
    /// </summary>
    Task SaveConfigurationAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports configuration to JSON format
    /// </summary>
    Task<string> ExportToJsonAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports configuration from JSON format
    /// </summary>
    Task<FrpClientConfig?> ImportFromJsonAsync(string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default configuration file path
    /// </summary>
    string GetDefaultConfigPath();

    /// <summary>
    /// Gets the application data directory
    /// </summary>
    string GetAppDataDirectory();
}
