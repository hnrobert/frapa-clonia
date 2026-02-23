using FrapaClonia.Core.Interfaces;
using FrapaClonia.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing application localization using .NET ResourceManager
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly ISettingsService? _settingsService;

    public CultureInfo CurrentCulture { get; private set; }
    public List<CultureInfo> SupportedCultures { get; }

    public event EventHandler? CultureChanged;

    public LocalizationService(ILogger<LocalizationService> logger, ISettingsService? settingsService = null)
    {
        _logger = logger;
        _settingsService = settingsService;

        // Define supported cultures
        SupportedCultures =
        [
            new CultureInfo("en"), // English (default)
            new CultureInfo("zh"), // Chinese Simplified
            new CultureInfo("ja"), // Japanese
            new CultureInfo("ko"), // Korean
            new CultureInfo("es"), // Spanish
            new CultureInfo("fr"), // French
            new CultureInfo("de"), // German
            new CultureInfo("ru") // Russian
        ];

        // Try to load saved language from settings
        var savedCulture = LoadSavedCulture();

        // If no saved culture, auto-detect system language
        var systemCulture = savedCulture ?? CultureInfo.CurrentUICulture;
        var supportedCulture = SupportedCultures
            .FirstOrDefault(c =>
                c.Name == systemCulture.Name || c.Name.StartsWith(systemCulture.TwoLetterISOLanguageName));

        CurrentCulture = supportedCulture ?? SupportedCultures[0];

        // Apply the culture immediately
        ApplyCulture(CurrentCulture);

        _logger.LogInformation("Localization initialized with culture: {Culture}", CurrentCulture.Name);
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    private CultureInfo? LoadSavedCulture()
    {
        try
        {
            // Load from settings service if available
            if (_settingsService != null)
            {
                _settingsService.LoadAsync().GetAwaiter().GetResult();
                var languageCode = _settingsService.Settings.Language;

                if (!string.IsNullOrEmpty(languageCode))
                {
                    var culture = SupportedCultures.FirstOrDefault(c => c.Name == languageCode);
                    if (culture != null)
                    {
                        _logger.LogInformation("Loaded saved language: {Language}", languageCode);
                        return culture;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load saved language from settings");
        }

        return null;
    }

    public void SetCulture(string cultureCode)
    {
        var culture = SupportedCultures.FirstOrDefault(c => c.Name == cultureCode);
        if (culture == null) return;
        CurrentCulture = culture;
        ApplyCulture(culture);
        CultureChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Culture changed to: {Culture}", culture.Name);
    }

    public string GetString(string key, params object[] args)
    {
        try
        {
            // Use ResourceManager to get the localized string
            var value = Strings.ResourceManager.GetString(key, CurrentCulture);

            // Fallback to English if not found in current culture
            if (string.IsNullOrEmpty(value) && CurrentCulture.Name != "en")
            {
                value = Strings.ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en"));
            }

            // Return key if not found
            if (!string.IsNullOrEmpty(value))
                return args.Length > 0 ? string.Format(CurrentCulture, value, args) : value;
            _logger.LogWarning("Localization key not found: {Key}", key);
            return key;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized string for key: {Key}", key);
            return key;
        }
    }
}
