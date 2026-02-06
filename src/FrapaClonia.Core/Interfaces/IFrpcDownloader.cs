namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for downloading frpc binaries
/// </summary>
public interface IFrpcDownloader
{
    /// <summary>
    /// Gets available frpc versions from GitHub
    /// </summary>
    Task<IReadOnlyList<FrpRelease>> GetAvailableVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a specific version of frpc
    /// </summary>
    Task<string> DownloadVersionAsync(FrpRelease release, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest frpc version
    /// </summary>
    Task<FrpRelease?> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads frpc from a mirror site
    /// </summary>
    Task<string> DownloadFromMirrorAsync(string mirrorUrl, string targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a frp release
/// </summary>
public class FrpRelease
{
    public required string TagName { get; init; }
    public required string Version { get; init; }
    public required string HtmlUrl { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public required List<FrpAsset> Assets { get; init; }
}

/// <summary>
/// Asset information for a release
/// </summary>
public class FrpAsset
{
    public required string Name { get; init; }
    public required string DownloadUrl { get; init; }
    public required long Size { get; init; }
    public required string Platform { get; init; }  // windows, linux, darwin
    public required List<string> Architecture { get; init; }  // amd64, arm64, etc.
}
