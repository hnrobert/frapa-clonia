using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing configuration profiles
/// </summary>
public class ProfileService(ILogger<ProfileService> logger) : IProfileService
{
    private readonly string _profilesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FrapaClonia",
        "profiles");

    private readonly string _activeProfileFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FrapaClonia",
        "active_profile.txt");

    public async Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_profilesDir))
            {
                Directory.CreateDirectory(_profilesDir);
                return new List<ProfileInfo>();
            }

            var activeProfile = await GetActiveProfileAsync(cancellationToken);
            var profiles = new List<ProfileInfo>();

            foreach (var profileFile in Directory.GetFiles(_profilesDir, "*.json"))
            {
                var profileName = Path.GetFileNameWithoutExtension(profileFile);
                var profileInfo = new ProfileInfo
                {
                    Name = profileName,
                    Description = $"Profile: {profileName}",
                    LastModified = File.GetLastWriteTime(profileFile),
                    IsActive = profileName == activeProfile,
                    ProxyCount = 0 // Could be populated from config file
                };
                profiles.Add(profileInfo);
            }

            logger.LogInformation("Found {Count} profiles", profiles.Count);
            return profiles;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profiles");
            return new List<ProfileInfo>();
        }
    }

    public async Task<Profile?> LoadProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var profilePath = Path.Combine(_profilesDir, $"{profileName}.json");
            if (!File.Exists(profilePath))
            {
                logger.LogWarning("Profile not found: {ProfileName}", profileName);
                return null;
            }

            var json = await File.ReadAllTextAsync(profilePath, cancellationToken);
            var profile = JsonSerializer.Deserialize(json, ProfileContext.Default.Profile);

            logger.LogInformation("Loaded profile: {ProfileName}", profileName);
            return profile;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading profile: {ProfileName}", profileName);
            return null;
        }
    }

    public async Task SaveProfileAsync(string profileName, Profile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_profilesDir))
            {
                Directory.CreateDirectory(_profilesDir);
            }

            var profilePath = Path.Combine(_profilesDir, $"{profileName}.json");

            // Create a new profile with the correct name to handle init-only properties
            var profileToSave = new Profile
            {
                Name = profileName,
                Description = profile.Description,
                ConfigPath = profile.ConfigPath,
                Metadata = profile.Metadata
            };

            var json = JsonSerializer.Serialize(profileToSave, ProfileContext.Default.Profile);
            await File.WriteAllTextAsync(profilePath, json, cancellationToken);

            logger.LogInformation("Saved profile: {ProfileName}", profileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving profile: {ProfileName}", profileName);
        }
    }

    public async Task DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var profilePath = Path.Combine(_profilesDir, $"{profileName}.json");
            if (File.Exists(profilePath))
            {
                await Task.Run(() => File.Delete(profilePath), cancellationToken);
                logger.LogInformation("Deleted profile: {ProfileName}", profileName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting profile: {ProfileName}", profileName);
        }
    }

    public async Task<string?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_activeProfileFile))
            {
                var activeProfile = await File.ReadAllTextAsync(_activeProfileFile, cancellationToken);
                return activeProfile.Trim();
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active profile");
            return null;
        }
    }

    public async Task SetActiveProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var appDataDir = Path.GetDirectoryName(_activeProfileFile);
            if (!string.IsNullOrEmpty(appDataDir) && !Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            await File.WriteAllTextAsync(_activeProfileFile, profileName, cancellationToken);
            logger.LogInformation("Set active profile: {ProfileName}", profileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting active profile: {ProfileName}", profileName);
        }
    }

    public async Task<Profile> CreateDefaultProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var profile = new Profile
        {
            Name = profileName,
            Description = "Default profile",
            ConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FrapaClonia",
                "frpc.toml"),
            Metadata = new Dictionary<string, string>
            {
                { "Created", DateTime.UtcNow.ToString("O") },
                { "IsDefault", "true" }
            }
        };

        await SaveProfileAsync(profileName, profile, cancellationToken);
        logger.LogInformation("Created default profile: {ProfileName}", profileName);

        return profile;
    }
}
