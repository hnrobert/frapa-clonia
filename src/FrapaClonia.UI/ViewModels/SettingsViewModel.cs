using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for application settings
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IAutoStartService _autoStartService;

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    [ObservableProperty]
    private bool _autoStartEnabled;

    [ObservableProperty]
    private bool _portableMode;

    [ObservableProperty]
    private string _configLocation = "";

    [ObservableProperty]
    private bool _isSaving;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }

    public List<LanguageOption> AvailableLanguages { get; }

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ILocalizationService localizationService,
        IAutoStartService autoStartService)
    {
        _logger = logger;
        _localizationService = localizationService;
        _autoStartService = autoStartService;

        SaveCommand = new RelayCommand(async () => await SaveAsync());
        ResetCommand = new RelayCommand(async () => await LoadSettingsAsync());

        AvailableLanguages = new List<LanguageOption>
        {
            new("en", "English"),
            new("zh-CN", "简体中文"),
            new("ja", "日本語"),
            new("ko", "한국어"),
            new("es", "Español"),
            new("fr", "Français"),
            new("de", "Deutsch"),
            new("ru", "Русский")
        };

        _localizationService.CultureChanged += (s, e) =>
        {
            var cultureCode = _localizationService.CurrentCulture.Name;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                ?? AvailableLanguages.First();
        };

        _ = Task.Run(LoadSettingsAsync);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var cultureCode = _localizationService.CurrentCulture.Name;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == cultureCode)
                ?? AvailableLanguages.First();
            AutoStartEnabled = await _autoStartService.IsAutoStartEnabledAsync();
            PortableMode = DetectPortableMode();
            ConfigLocation = GetConfigLocation();

            _logger.LogInformation("Settings loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            // Apply language change
            if (SelectedLanguage != null && SelectedLanguage.Code != _localizationService.CurrentCulture.Name)
            {
                _localizationService.SetCulture(SelectedLanguage.Code);
            }

            // Apply auto-start setting
            if (AutoStartEnabled)
            {
                await _autoStartService.EnableAutoStartAsync();
            }
            else
            {
                await _autoStartService.DisableAutoStartAsync();
            }

            // TODO: Save portable mode setting
            // TODO: Save other settings

            _logger.LogInformation("Settings saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool DetectPortableMode()
    {
        // Check if running in portable mode
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
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "FrapaClonia");
        }
    }
}

/// <summary>
/// Language option for selection
/// </summary>
public record LanguageOption(string Code, string Name);
