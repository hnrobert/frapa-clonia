using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing configuration profiles
/// </summary>
public class ProfileService(ILogger<ProfileService> logger) : IProfileService
{
    // ReSharper disable once UnusedMember.Local
    private readonly ILogger<ProfileService> _logger = logger;

    public Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult<IReadOnlyList<ProfileInfo>>(new List<ProfileInfo>());
    }

    public Task<Profile?> LoadProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult<Profile?>(new Profile { Name = profileName, ConfigPath = "" });
    }

    public Task SaveProfileAsync(string profileName, Profile profile, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.CompletedTask;
    }

    public Task DeleteProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.CompletedTask;
    }

    public Task<string?> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult<string?>(null);
    }

    public Task SetActiveProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.CompletedTask;
    }

    public Task<Profile> CreateDefaultProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Phase 2
        return Task.FromResult(new Profile { Name = profileName, ConfigPath = "" });
    }
}
