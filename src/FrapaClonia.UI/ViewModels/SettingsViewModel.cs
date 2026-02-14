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
    private readonly ToastService? _toastService;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IPresetService? _presetService;

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

    // Frpc Version Management
    [ObservableProperty] private List<DownloadedFrpcVersion> _downloadedVersions = [];
    [ObservableProperty] private bool _isLoadingVersions;
    [ObservableProperty] private DownloadedFrpcVersion? _selectedVersion;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand RefreshVersionsCommand { get; }
    public IRelayCommand DeleteVersionCommand { get; }

    public List<LanguageOption> AvailableLanguages { get; }

    public List<ThemeOption> AvailableThemes { get; }

    // Default constructor for design-time support
    public SettingsViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<SettingsViewModel>.Instance,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!)
    {
    }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ILocalizationService localizationService,
        IAutoStartService autoStartService,
        ThemeService themeService,
        ToastService? toastService,
        INativeDeploymentService? nativeDeploymentService,
        IPresetService? presetService)
    {
        _logger = logger;
        _localizationService = localizationService;
        _autoStartService = autoStartService;
        _themeService = themeService;
        _toastService = toastService;
        _nativeDeploymentService = nativeDeploymentService;
        _presetService = presetService;

        AvailableLanguages =
        [
            new LanguageOption("en", "English"),
            new LanguageOption("zh-CN", "简体中文"),
            new LanguageOption("zh-TW", "繁體中文"),
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
        RefreshVersionsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RefreshDownloadedVersionsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error refreshing downloaded versions");
            }
        });
        DeleteVersionCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DeleteSelectedVersionAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error deleting version");
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

        // Initialize available themes
        AvailableThemes =
        [
            new ThemeOption(0, "Light", _localizationService),
            new ThemeOption(1, "Dark", _localizationService),
            new ThemeOption(2, "SystemDefault", _localizationService)
        ];

        // Load saved settings on initialization
        _ = Task.Run(async () =>
        {
            await LoadSettingsAsync();
            await RefreshDownloadedVersionsAsync();
        });
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

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

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value == null || _localizationService == null ||
            value.Code == _localizationService.CurrentCulture.Name) return;
        _localizationService.SetCulture(value.Code);
        _logger?.LogInformation("Language changed to: {Language}", value.Code);
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
                    _logger?.LogInformation("Settings file loaded from: {SettingsFile}", _settingsFile);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not load settings file, using defaults");
                }
            }

            // Apply settings or use defaults
            var cultureCode = settings?.Language ?? "en";
            var languageOption = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                                  ?? AvailableLanguages.First();

            // Apply language setting immediately
            SelectedLanguage = languageOption;
            if (_localizationService != null && cultureCode != _localizationService.CurrentCulture.Name)
            {
                _localizationService.SetCulture(cultureCode);
                _logger?.LogInformation("Loaded language setting: {Language}", cultureCode);
            }

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

            _logger?.LogInformation("Settings loaded: Language={Language}, Theme={Theme}", cultureCode, themeStr);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading settings");
            _toastService?.Error("Load Failed", "Could not load settings");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

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
            _toastService?.Success("Saved", "Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving settings");
            _toastService?.Error("Save Failed", $"Could not save settings: {ex.Message}");
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

    #region Frpc Version Management

    private async Task RefreshDownloadedVersionsAsync()
    {
        if (_nativeDeploymentService == null) return;

        try
        {
            IsLoadingVersions = true;
            _logger?.LogInformation("Refreshing downloaded frpc versions");

            var versions = await _nativeDeploymentService.GetDownloadedVersionsAsync();

            // Check which versions are in use by presets
            var usedPaths = await GetUsedBinaryPathsAsync();
            foreach (var version in versions)
            {
                version.IsInUse = usedPaths.Contains(version.BinaryPath);
            }

            DownloadedVersions = versions.ToList();
            _logger?.LogInformation("Found {Count} downloaded frpc versions", DownloadedVersions.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing downloaded versions");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotLoadVersions"));
        }
        finally
        {
            IsLoadingVersions = false;
        }
    }

    private async Task<HashSet<string>> GetUsedBinaryPathsAsync()
    {
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (_presetService != null)
            {
                // Access the Presets collection directly
                foreach (var preset in _presetService.Presets)
                {
                    if (!string.IsNullOrEmpty(preset.Deployment.FrpcBinaryPath))
                    {
                        usedPaths.Add(preset.Deployment.FrpcBinaryPath);
                    }
                }
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error getting used binary paths");
        }

        return usedPaths;
    }

    private async Task DeleteSelectedVersionAsync()
    {
        if (SelectedVersion == null)
        {
            _toastService?.Warning(L("Toast_NoSelection"), L("Toast_SelectVersionToDelete"));
            return;
        }

        if (SelectedVersion.IsInUse)
        {
            _toastService?.Warning(L("Toast_VersionInUse"), L("Toast_CannotDeleteUsedVersion"));
            return;
        }

        if (_nativeDeploymentService == null) return;

        try
        {
            var success = await _nativeDeploymentService.DeleteVersionAsync(SelectedVersion.FolderPath);
            if (success)
            {
                _toastService?.Success(L("Toast_Deleted"), L("Toast_VersionDeleted", SelectedVersion.Version));
                await RefreshDownloadedVersionsAsync();
                SelectedVersion = null;
            }
            else
            {
                _toastService?.Error(L("Toast_DeleteFailed"), L("Toast_CouldNotDeleteVersion"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting version");
            _toastService?.Error(L("Toast_Error"), L("Toast_DeleteFailedWithError", ex.Message));
        }
    }

    public void Initialize()
    {
        _ = RefreshDownloadedVersionsAsync();
    }

    #endregion
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

/// <summary>
/// Theme option for selection
/// </summary>
public class ThemeOption : ObservableObject
{
    private readonly ILocalizationService? _localizationService;
    private readonly string _resourceKey;

    public int Index { get; }

    public string Name => _localizationService?.GetString(_resourceKey) ?? _resourceKey;

    public ThemeOption(int index, string resourceKey, ILocalizationService? localizationService)
    {
        Index = index;
        _resourceKey = resourceKey;
        _localizationService = localizationService;

        if (_localizationService != null)
        {
            _localizationService.CultureChanged += (_, _) => OnPropertyChanged(nameof(Name));
        }
    }
}