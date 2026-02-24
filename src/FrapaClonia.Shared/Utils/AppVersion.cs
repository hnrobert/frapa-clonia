using System.Reflection;

namespace FrapaClonia.Shared.Utils;

/// <summary>
/// Provides application version information
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Gets the application version string.
    /// In Debug mode, returns version + git commit SHA (first 7 characters), e.g., "0.0.1+e5fa43d".
    /// In Release mode, returns the semantic version without build metadata.
    /// </summary>
    public static string Version
    {
        get
        {
            if (field != null)
                return field;

            var informationalVersion = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrEmpty(informationalVersion))
            {
                field = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
                return field;
            }

            var plusIndex = informationalVersion.IndexOf('+');

#if DEBUG
            // Debug: Show version + git commit SHA (first 7 chars), e.g., "0.0.1+e5fa43d"
            if (plusIndex > 0 && informationalVersion.Length > plusIndex + 7)
            {
                var version = informationalVersion[..plusIndex];
                var sha = informationalVersion.Substring(plusIndex + 1, 7);
                field = $"{version}+{sha}";
            }
            else
            {
                field = informationalVersion;
            }
#else
            // Release: Strip build metadata (everything after '+')
            field = plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
#endif

            return field;
        }
    }

    /// <summary>
    /// Gets the copyright information
    /// </summary>
    public static string Copyright
    {
        get
        {
            var copyrightAttr = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyCopyrightAttribute>();
            return copyrightAttr?.Copyright ?? "© 2025 Robert He";
        }
    }
}
