namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for managing configuration profiles
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets all available profiles
    /// </summary>
    Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a specific profile
    /// </summary>
    Task<Profile?> LoadProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a profile
    /// </summary>
    Task SaveProfileAsync(string profileName, Profile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile
    /// </summary>
    Task DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active profile name
    /// </summary>
    Task<string?> GetActiveProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active profile
    /// </summary>
    Task SetActiveProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new profile with default settings
    /// </summary>
    Task<Profile> CreateDefaultProfileAsync(string profileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Profile information
/// </summary>
public class ProfileInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTime LastModified { get; init; }
    public bool IsActive { get; init; }
    public int ProxyCount { get; init; }
}

/// <summary>
/// Configuration profile
/// </summary>
public class Profile
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ConfigPath { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}
