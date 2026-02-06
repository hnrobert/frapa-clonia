namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for configuring auto-start on boot
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Gets whether auto-start is enabled
    /// </summary>
    Task<bool> IsAutoStartEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables auto-start on boot
    /// </summary>
    Task EnableAutoStartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables auto-start on boot
    /// </summary>
    Task DisableAutoStartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether auto-start is supported on the current platform
    /// </summary>
    bool IsAutoStartSupported { get; }
}
