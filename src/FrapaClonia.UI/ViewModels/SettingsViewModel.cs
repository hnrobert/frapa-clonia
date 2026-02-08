using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for application settings
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel>? _logger;
    private readonly ILocalizationService? _localizationService;
    private readonly IAutoStartService? _autoStartService;
    private readonly ThemeService? _themeService;

    private readonly string _settingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FrapaClonia",
        "settings.json");

    [ObservableProperty] private LanguageOption? _selectedLanguage;

    [ObservableProperty] private bool _autoStartEnabled;

    [ObservableProperty] private bool _portableMode;

    [ObservableProperty] private string _configLocation = "";

    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private int _themeIndex;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }

    public List<LanguageOption> AvailableLanguages { get; }

    // Default constructor for design-time support
    public SettingsViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<SettingsViewModel>.Instance,
        null!,
        null!,
        null!)
    {
    }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ILocalizationService localizationService,
        IAutoStartService autoStartService,
        ThemeService themeService)
    {
        _logger = logger;
        _localizationService = localizationService;
        _autoStartService = autoStartService;
        _themeService = themeService;

        AvailableLanguages =
        [
            new LanguageOption("en", "English"),
            new LanguageOption("zh-CN", "简体中文"),
            new LanguageOption("ja", "日本語"),
            new LanguageOption("ko", "한국어"),
            new LanguageOption("es", "Español"),
            new LanguageOption("fr", "Français"),
            new LanguageOption("de", "Deutsch"),
            new LanguageOption("ru", "Русский")
        ];

        SaveCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error saving settings");
            }
        });
        ResetCommand = new RelayCommand(async void () =>
        {
            try
            {
                await LoadSettingsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error loading settings");
            }
        });

        // Initialize theme from ThemeService
        ThemeIndex = _themeService.CurrentTheme.ToString() switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => 2
        };

        _localizationService.CultureChanged += (_, _) =>
        {
            var cultureCode = _localizationService.CurrentCulture.Name;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                               ?? AvailableLanguages.First();
        };

        // Initialize selected language from service - run on UI thread
        try
        {
            if (_localizationService != null)
            {
                var cultureCode = _localizationService.CurrentCulture.Name;
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                                   ?? AvailableLanguages.First();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading settings");
        }
    }

    partial void OnThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            0 => ThemeVariant.Light,
            1 => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        _themeService?.CurrentTheme = theme;
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Try to load settings from file
            AppSettings? settings = null;
            if (File.Exists(_settingsFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsFile);
                    settings = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not load settings file, using defaults");
                }
            }

            // Apply settings or use defaults
            var cultureCode = settings?.Language ?? "en";
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                               ?? AvailableLanguages.First();

            if (_autoStartService != null) AutoStartEnabled = await _autoStartService.IsAutoStartEnabledAsync();
            PortableMode = DetectPortableMode();
            ConfigLocation = GetConfigLocation();

            // Set theme from settings
            var themeStr = settings?.Theme ?? "Default";
            ThemeIndex = themeStr switch
            {
                "Light" => 0,
                "Dark" => 1,
                _ => 2
            };

            _logger?.LogInformation("Settings loaded");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading settings");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            // Apply language change
            if (SelectedLanguage != null && _localizationService != null &&
                SelectedLanguage.Code != _localizationService.CurrentCulture.Name)
            {
                _localizationService.SetCulture(SelectedLanguage.Code);
            }

            // Apply auto-start setting
            if (AutoStartEnabled)
            {
                if (_autoStartService != null) await _autoStartService.EnableAutoStartAsync();
            }
            else
            {
                if (_autoStartService != null) await _autoStartService.DisableAutoStartAsync();
            }

            // Save settings to file
            var settings = new AppSettings
            {
                Language = SelectedLanguage?.Code ?? "en",
                Theme = ThemeIndex switch
                {
                    0 => "Light",
                    1 => "Dark",
                    _ => "Default"
                },
                AutoStart = AutoStartEnabled,
                PortableMode = PortableMode
            };

            // Ensure directory exists
            var settingsDir = Path.GetDirectoryName(_settingsFile);
            if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            var json = JsonSerializer.Serialize(settings, AppSettingsContext.Default.AppSettings);
            await File.WriteAllTextAsync(_settingsFile, json);

            _logger?.LogInformation("Settings saved to: {SettingsFile}", _settingsFile);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving settings");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private static bool DetectPortableMode()
    {
        // Check if running in portable mode,
        // Portable mode is detected if the executable is in a directory with a config file
        var appDir = AppContext.BaseDirectory;
        var portableMarker = Path.Combine(appDir, "portable.txt");
        return File.Exists(portableMarker);
    }

    private string GetConfigLocation()
    {
        if (PortableMode)
        {
            return Path.Combine(AppContext.BaseDirectory, "config");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia");
    }
}

/// <summary>
/// Language option for selection
/// </summary>
public record LanguageOption(string Code, string Name);

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    public string Language { get; init; } = "en";
    public string Theme { get; init; } = "Default";
    public bool AutoStart { get; init; }
    public bool PortableMode { get; init; }
}