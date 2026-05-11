using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using FrapaClonia.Shared.Utils;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for application settings
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private const string Owner = "hnrobert";
    private const string Repo = "frapa-clonia";

    private readonly ILogger<SettingsViewModel>? _logger;
    private readonly ILocalizationService? _localizationService;
    private readonly IAutoStartService? _autoStartService;
    private readonly ISettingsService? _settingsService;
    private readonly ThemeService? _themeService;
    private readonly ToastService? _toastService;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IPresetService? _presetService;
    private readonly ICacheService? _cacheService;
    private readonly IUpdateService? _updateService;

    [ObservableProperty] private LanguageOption? _selectedLanguage;

    [ObservableProperty] private bool _autoStartEnabled;

    [ObservableProperty] private string _configLocation = "";

    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private int _themeIndex;

    // Frpc Version Management
    [ObservableProperty] private List<DownloadedFrpcVersion> _downloadedVersions = [];
    [ObservableProperty] private bool _isLoadingVersions;
    [ObservableProperty] private DownloadedFrpcVersion? _selectedVersion;

    // GitHub Integration
    [ObservableProperty] private GitHubTokenStatus _gitHubTokenStatus;
    [ObservableProperty] private string _gitHubTokenInput = "";
    [ObservableProperty] private bool _isGitHubUpdating;
    [ObservableProperty] private bool _isTokenVisible;
    [ObservableProperty] private bool _isVerifyingToken;

    // Updates
    [ObservableProperty] private string _currentVersion = AppVersion.Version;
    [ObservableProperty] private string _latestVersion = "";
    [ObservableProperty] private bool _isCheckingForUpdates;
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string _updateReleaseNotes = "";
    [ObservableProperty] private string _updateDownloadUrl = "";

    public bool IsGitHubLoggedIn => GitHubTokenStatus != GitHubTokenStatus.None;
    public bool IsGitHubConnected => GitHubTokenStatus == GitHubTokenStatus.Connected;
    public bool IsGitHubTokenInvalid => GitHubTokenStatus == GitHubTokenStatus.Invalid;
    public char TokenPasswordChar => IsTokenVisible ? '\0' : '•';

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IRelayCommand OpenConfigFolderCommand { get; }
    public IRelayCommand GitHubLoginCommand { get; }
    public IRelayCommand GitHubConnectCommand { get; }
    public IRelayCommand GitHubLogoutCommand { get; }
    public IRelayCommand GitHubUpdateCommand { get; }
    public IRelayCommand GitHubVerifyCommand { get; }
    public IRelayCommand GitHubVerifyCachedCommand { get; }
    public IRelayCommand GitHubToggleTokenVisibilityCommand { get; }
    public IRelayCommand GitHubCancelUpdateCommand { get; }
    public IRelayCommand CheckForUpdatesCommand { get; }
    public IRelayCommand DownloadUpdateCommand { get; }
    public IRelayCommand OpenReleasePageCommand { get; }

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
        ISettingsService settingsService,
        ThemeService themeService,
        ToastService? toastService,
        INativeDeploymentService? nativeDeploymentService,
        IPresetService? presetService,
        ICacheService? cacheService,
        IUpdateService? updateService)
    {
        _logger = logger;
        _localizationService = localizationService;
        _autoStartService = autoStartService;
        _settingsService = settingsService;
        _themeService = themeService;
        _toastService = toastService;
        _nativeDeploymentService = nativeDeploymentService;
        _presetService = presetService;
        _cacheService = cacheService;
        _updateService = updateService;

        AvailableLanguages =
        [
            new LanguageOption("en", "English"),
            new LanguageOption("zh", "简体中文"),
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
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
        GitHubLoginCommand = new RelayCommand(OpenGitHubTokenPage);
        GitHubConnectCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ConnectGitHubAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error connecting GitHub");
            }
        });
        GitHubLogoutCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DisconnectGitHubAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error disconnecting GitHub");
            }
        });
        GitHubUpdateCommand = new RelayCommand(() =>
        {
            IsGitHubUpdating = true;
            IsTokenVisible = false;
            GitHubTokenInput = _cacheService?.GitHubToken ?? "";
        });
        GitHubVerifyCommand = new RelayCommand(async void () =>
        {
            try
            {
                await VerifyTokenAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error verifying GitHub token");
            }
        });
        GitHubVerifyCachedCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ValidateGitHubTokenAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error verifying cached GitHub token");
            }
        });
        GitHubToggleTokenVisibilityCommand = new RelayCommand(() =>
        {
            IsTokenVisible = !IsTokenVisible;
        });
        GitHubCancelUpdateCommand = new RelayCommand(() =>
        {
            IsGitHubUpdating = false;
            GitHubTokenInput = "";
            IsTokenVisible = false;
        });
        CheckForUpdatesCommand = new RelayCommand(async void () =>
        {
            try { await CheckForUpdatesAsync(isManual: true); }
            catch (Exception e) { _logger?.LogError(e, "Error checking for updates"); }
        });
        DownloadUpdateCommand = new RelayCommand(() => OpenUrl(_updateDownloadUrl));
        OpenReleasePageCommand = new RelayCommand(() =>
        {
            if (UpdateAvailable && !string.IsNullOrEmpty(_updateDownloadUrl))
            {
                // Open the release page (strip the asset download URL to get the release page)
                var tag = $"releases/tag/{LatestVersion}";
                OpenUrl($"https://github.com/{Owner}/{Repo}/{tag}");
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
            new ThemeOption("Light", _localizationService),
            new ThemeOption("Dark", _localizationService),
            new ThemeOption("SystemDefault", _localizationService)
        ];

        // Load saved settings on initialization
        _ = Task.Run(async () =>
        {
            await LoadSettingsAsync();
            await RefreshDownloadedVersionsAsync();

            // Token validation with throttling
            if (!string.IsNullOrEmpty(_cacheService?.GitHubToken))
            {
                var lastValidation = _cacheService.LastGitHubTokenValidation;
                if (lastValidation == null || (DateTime.UtcNow - lastValidation.Value).TotalHours > 24)
                {
                    await ValidateGitHubTokenAsync();
                }
                else
                {
                    GitHubTokenStatus = GitHubTokenStatus.Connected;
                }
            }

            // Auto-check for updates (>24h since last check)
            if (_updateService != null)
            {
                var lastCheck = _cacheService?.LastSelfUpdateCheck;
                if (lastCheck == null || (DateTime.UtcNow - lastCheck.Value).TotalHours > 24)
                {
                    await CheckForUpdatesAsync();
                }
            }
        });
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnGitHubTokenStatusChanged(GitHubTokenStatus value)
    {
        OnPropertyChanged(nameof(IsGitHubLoggedIn));
        OnPropertyChanged(nameof(IsGitHubConnected));
        OnPropertyChanged(nameof(IsGitHubTokenInvalid));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsTokenVisibleChanged(bool value) => OnPropertyChanged(nameof(TokenPasswordChar));

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
            // Load settings from service
            if (_settingsService != null)
            {
                await _settingsService.LoadAsync();
            }

            var settings = _settingsService?.Settings ?? new AppSettings();

            // Apply settings
            var cultureCode = settings.Language;
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
            ConfigLocation = GetConfigLocation();

            // Set theme from settings
            var themeStr = settings.Theme;
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

            // Apply pending version deletions
            await ApplyPendingDeletionsAsync();

            // Update settings via service
            if (_settingsService != null)
            {
                _settingsService.Settings.Language = SelectedLanguage?.Code ?? "en";
                _settingsService.Settings.Theme = ThemeIndex switch
                {
                    0 => "Light",
                    1 => "Dark",
                    _ => "Default"
                };
                _settingsService.Settings.AutoStart = AutoStartEnabled;

                await _settingsService.SaveAsync();
            }

            // Refresh version list after deletions
            await RefreshDownloadedVersionsAsync();

            _logger?.LogInformation("Settings saved successfully");
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

    private static string GetConfigLocation()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia");
    }

    private void OpenConfigFolder()
    {
        try
        {
            var path = ConfigLocation;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            _logger?.LogInformation("Opened config folder: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening config folder");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotOpenFolder"));
        }
    }

    private void OpenUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening URL: {Url}", url);
        }
    }

    private async Task CheckForUpdatesAsync(bool isManual = false)
    {
        if (_updateService == null) return;

        try
        {
            IsCheckingForUpdates = true;
            var update = await _updateService.CheckForUpdatesAsync();

            if (_cacheService != null)
            {
                _cacheService.LastSelfUpdateCheck = DateTime.UtcNow;
                await _cacheService.SaveAsync();
            }

            if (update != null)
            {
                UpdateAvailable = true;
                LatestVersion = update.Version;
                UpdateReleaseNotes = update.ReleaseNotes ?? "";
                UpdateDownloadUrl = update.DownloadUrl ?? update.HtmlUrl ?? "";
                _logger?.LogInformation("Update available: {Version}", update.Version);
                _toastService?.Info(L("UpdateAvailable"), $"v{update.Version}");
            }
            else if (isManual)
            {
                UpdateAvailable = false;
                _toastService?.Success(L("UpToDate"), L("UpToDateDesc"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking for updates");
            if (isManual)
                _toastService?.Error(L("UpdateCheckFailed"), L("UpdateCheckFailedDesc"));
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    #region GitHub Integration

    private async Task ValidateGitHubTokenAsync()
    {
        var token = _cacheService?.GitHubToken;
        if (string.IsNullOrEmpty(token)) return;

        try
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("FrapaClonia"))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await client.User.Current();
            GitHubTokenStatus = GitHubTokenStatus.Connected;
            _logger?.LogInformation("GitHub token validated successfully");
            _toastService?.Success(L("Toast_GitHubTokenValid"), L("Toast_GitHubTokenValidDesc"));

            if (_cacheService != null)
            {
                _cacheService.LastGitHubTokenValidation = DateTime.UtcNow;
                await _cacheService.SaveAsync();
            }
        }
        catch (Octokit.AuthorizationException)
        {
            GitHubTokenStatus = GitHubTokenStatus.Invalid;
            _logger?.LogWarning("GitHub token is invalid or expired");
            _toastService?.Warning(L("Toast_GitHubTokenExpired"), L("Toast_GitHubTokenExpiredDesc"));
        }
        catch (Exception ex)
        {
            // Network errors — assume still valid, don't notify
            GitHubTokenStatus = GitHubTokenStatus.Connected;
            _logger?.LogDebug(ex, "Could not validate GitHub token (network error)");
        }
    }

    private void OpenGitHubTokenPage()
    {
        try
        {
            var url = "https://github.com/settings/tokens/new?description=FrapaClonia&scopes=public_repo";
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            _logger?.LogInformation("Opened GitHub token creation page");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening GitHub token page");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotOpenUrl"));
        }
    }

    private async Task ConnectGitHubAsync()
    {
        var token = GitHubTokenInput.Trim();
        if (string.IsNullOrEmpty(token))
        {
            _toastService?.Warning(L("Toast_Warning"), L("Toast_GitHubTokenInvalid"));
            return;
        }

        try
        {
            IsVerifyingToken = true;
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("FrapaClonia"))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await client.User.Current();

            if (_cacheService != null)
            {
                _cacheService.GitHubToken = token;
                await _cacheService.SaveAsync();
            }

            GitHubTokenStatus = GitHubTokenStatus.Connected;
            IsGitHubUpdating = false;
            GitHubTokenInput = "";
            _logger?.LogInformation("GitHub token saved successfully");
            _toastService?.Success(L("Toast_Success"), L("Toast_GitHubConnected"));
        }
        catch (Octokit.AuthorizationException)
        {
            _toastService?.Error(L("Toast_GitHubTokenInvalid"), L("Toast_GitHubTokenInvalid"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GitHub token validation failed");
            _toastService?.Error(L("Toast_Error"), L("Toast_GitHubTokenInvalid"));
        }
        finally
        {
            IsVerifyingToken = false;
        }
    }

    private async Task VerifyTokenAsync()
    {
        var token = GitHubTokenInput.Trim();
        if (string.IsNullOrEmpty(token))
        {
            _toastService?.Warning(L("Toast_Warning"), L("Toast_GitHubTokenInvalid"));
            return;
        }

        try
        {
            IsVerifyingToken = true;
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("FrapaClonia"))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await client.User.Current();
            _toastService?.Success(L("Toast_GitHubTokenValid"), L("Toast_GitHubTokenValidDesc"));
        }
        catch (Octokit.AuthorizationException)
        {
            _toastService?.Error(L("Toast_GitHubTokenInvalid"), L("Toast_GitHubTokenInvalid"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GitHub token verification failed");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotVerifyToken"));
        }
        finally
        {
            IsVerifyingToken = false;
        }
    }

    private async Task DisconnectGitHubAsync()
    {
        try
        {
            if (_cacheService != null)
            {
                _cacheService.GitHubToken = null;
                await _cacheService.SaveAsync();
            }

            GitHubTokenStatus = GitHubTokenStatus.None;
            IsGitHubUpdating = false;
            GitHubTokenInput = "";
            _logger?.LogInformation("GitHub token removed");
            _toastService?.Success(L("Toast_Success"), L("Toast_GitHubDisconnected"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disconnecting GitHub");
        }
    }

    #endregion

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

    private async Task ApplyPendingDeletionsAsync()
    {
        if (_nativeDeploymentService == null) return;

        var versionsToDelete = DownloadedVersions.Where(v => v.IsPendingDeletion).ToList();
        if (versionsToDelete.Count == 0) return;

        var deletedCount = 0;
        foreach (var version in versionsToDelete)
        {
            try
            {
                var success = await _nativeDeploymentService.DeleteVersionAsync(version.FolderPath);
                if (success)
                {
                    deletedCount++;
                }
                else
                {
                    _logger?.LogWarning("Failed to delete version {Version}", version.Version);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting version {Version}", version.Version);
            }
        }

        if (deletedCount > 0)
        {
            _logger?.LogInformation("Deleted {Count} frpc versions", deletedCount);
        }
    }

    #endregion
}

/// <summary>
/// GitHub token connection status
/// </summary>
public enum GitHubTokenStatus
{
    None,
    Connected,
    Invalid
}

/// <summary>
/// Language option for selection
/// </summary>
public record LanguageOption(string Code, string Name);

/// <summary>
/// Theme option for selection
/// </summary>
public class ThemeOption : ObservableObject
{
    private readonly ILocalizationService? _localizationService;

    public string Name => _localizationService?.GetString(field) ?? field;

    public ThemeOption(string resourceKey, ILocalizationService? localizationService)
    {
        Name = resourceKey;
        _localizationService = localizationService;

        if (_localizationService != null)
        {
            _localizationService.CultureChanged += (_, _) => OnPropertyChanged(nameof(Name));
        }
    }
}
