using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for downloading frpc binaries
/// </summary>
public class FrpcDownloader : IFrpcDownloader
{
    private readonly ILogger<FrpcDownloader> _logger;

    public FrpcDownloader(ILogger<FrpcDownloader> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<FrpRelease>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult<IReadOnlyList<FrpRelease>>(new List<FrpRelease>());
    }

    public Task<string> DownloadVersionAsync(FrpRelease release, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Downloading frpc version {Version}", release.Version);
        return Task.FromResult(string.Empty);
    }

    public Task<FrpRelease?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        return Task.FromResult<FrpRelease?>(null);
    }

    public Task<string> DownloadFromMirrorAsync(string mirrorUrl, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 3
        _logger.LogInformation("Downloading frpc from mirror {MirrorUrl}", mirrorUrl);
        return Task.FromResult(string.Empty);
    }
}
