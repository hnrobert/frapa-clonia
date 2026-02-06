using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for serializing/deserializing frpc.toml files
/// </summary>
public class TomlSerializer : ITomlSerializer
{
    private readonly ILogger<TomlSerializer> _logger;

    public TomlSerializer(ILogger<TomlSerializer> logger)
    {
        _logger = logger;
    }

    public Task<FrpClientConfig?> DeserializeAsync(string tomlContent, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Deserializing TOML content");
        return Task.FromResult<FrpClientConfig?>(new FrpClientConfig());
    }

    public Task<FrpClientConfig?> DeserializeFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Deserializing TOML file at {FilePath}", filePath);
        return Task.FromResult<FrpClientConfig?>(new FrpClientConfig());
    }

    public Task<string> SerializeAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Serializing configuration to TOML");
        return Task.FromResult(string.Empty);
    }

    public Task SerializeToFileAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        _logger.LogInformation("Serializing configuration to file at {FilePath}", filePath);
        return Task.CompletedTask;
    }
}
