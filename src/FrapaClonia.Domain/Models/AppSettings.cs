namespace FrapaClonia.Domain.Models;

/// <summary>
/// Application settings stored in settings.toml
/// </summary>
public class AppSettings
{
    /// <summary>
    /// UI language code (e.g., "en", "zh", "ja")
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Theme name: "Light", "Dark", or "Default"
    /// </summary>
    public string Theme { get; set; } = "Default";

    /// <summary>
    /// Whether to start the application automatically on system boot
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// Whether the application is running in portable mode
    /// </summary>
    public bool PortableMode { get; set; }
}
