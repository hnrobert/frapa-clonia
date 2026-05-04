using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// Dashboard view model for main application interface
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogger<DashboardViewModel>? _logger;
    private readonly IFrpcProcessService? _frpcProcessService;
    private readonly IConfigurationService? _configurationService;
    private readonly IValidationService? _validationService;
    private readonly IPresetService? _presetService;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;
    private readonly ISystemServiceManager? _systemServiceManager;

    [ObservableProperty] private bool _isFrpcRunning;

    [ObservableProperty] private int? _frpcProcessId;

    [ObservableProperty] private string _statusMessage = "Frpc is not running";

    [ObservableProperty] private string _lastLogLine = "";

    [ObservableProperty] private bool _hasConfiguration;

    [ObservableProperty] private string _serverAddress = "Not configured";

    [ObservableProperty] private int _proxyCount;

    // Service status
    [ObservableProperty] private bool _isNativeMode = true;
    [ObservableProperty] private bool _isServiceInstalled;
    [ObservableProperty] private bool _isServiceRunning;

    public bool NeedsServiceInstall => IsNativeMode && !IsServiceInstalled && !IsFrpcRunning;

    // Overview stats
    [ObservableProperty] private int _runningInstanceCount;
    [ObservableProperty] private string _currentPresetRuntime = "";
    [ObservableProperty] private int _totalActiveProxies;

    // Preset management properties
    [ObservableProperty] private string _presetName = "";

    [ObservableProperty] private bool _isRenaming;

    [ObservableProperty] private bool _canDeletePreset = true;

    private string _originalPresetName = "";

    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxyListCommand { get; }
    public IRelayCommand NavigateToDeploymentCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IRelayCommand NavigateToSettingsCommand { get; }
    public IRelayCommand NavigateToLogsCommand { get; }
    public IRelayCommand StartFrpcCommand { get; }
    public IRelayCommand StopFrpcCommand { get; }
    public IRelayCommand RestartFrpcCommand { get; }
    public IRelayCommand InstallServiceCommand { get; }

    // Preset management commands
    public IRelayCommand RenamePresetCommand { get; }
    public IRelayCommand CancelRenameCommand { get; }
    public IRelayCommand DeletePresetCommand { get; }
    public IRelayCommand DuplicatePresetCommand { get; }
    public IRelayCommand ExportTomlCommand { get; }
    public IRelayCommand ExportIniCommand { get; }
    public IRelayCommand ImportCommand { get; }

    // Default constructor for design-time support
    public DashboardViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DashboardViewModel>.Instance,
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

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IFrpcProcessService frpcProcessService,
        IConfigurationService configurationService,
        IValidationService validationService,
        IPresetService presetService,
        NavigationService navigationService,
        ToastService? toastService,
        ILocalizationService? localizationService,
        ISystemServiceManager? systemServiceManager)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _configurationService = configurationService;
        _validationService = validationService;
        _presetService = presetService;
        _toastService = toastService;
        _localizationService = localizationService;
        _systemServiceManager = systemServiceManager;

        // For design-time or when services are null, use empty commands
        NavigateToServerConfigCommand = new RelayCommand(() => navigationService.NavigateTo("server"));
        NavigateToProxyListCommand = new RelayCommand(() => navigationService.NavigateTo("proxies"));
        NavigateToDeploymentCommand = new RelayCommand(() => navigationService.NavigateTo("deployment"));
        NavigateToSettingsCommand = new RelayCommand(() => navigationService.NavigateTo("settings"));
        NavigateToLogsCommand = new RelayCommand(() => navigationService.NavigateTo("logs"));

        StartFrpcCommand = new RelayCommand(async void () =>
        {
            try
            {
                await StartFrpcAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not start frpc");
                _toastService?.Error("Start Failed", $"Could not start frpc: {e.Message}");
            }
        }, () => !IsFrpcRunning);
        StopFrpcCommand = new RelayCommand(async void () =>
        {
            try
            {
                await StopFrpcAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not stop frpc");
                _toastService?.Error("Stop Failed", $"Could not stop frpc: {e.Message}");
            }
        }, () => IsFrpcRunning);
        RestartFrpcCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RestartFrpcAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not restart frpc");
                _toastService?.Error("Restart Failed", $"Could not restart frpc: {e.Message}");
            }
        }, () => IsFrpcRunning);
        InstallServiceCommand = new RelayCommand(async void () =>
        {
            try
            {
                await InstallServiceAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not install service");
                _toastService?.Error("Install Failed", $"Could not install service: {e.Message}");
            }
        }, () => IsNativeMode && !IsServiceInstalled);

        // Preset management commands
        RenamePresetCommand = new RelayCommand(async void () =>
        {
            try
            {
                if (IsRenaming)
                {
                    // Save the rename
                    await RenamePresetAsync();
                }
                else
                {
                    // Enter renaming mode
                    IsRenaming = true;
                    // Store original name in case we need to cancel
                    _originalPresetName = PresetName;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not rename preset");
                _toastService?.Error("Rename Failed", $"Could not rename preset: {e.Message}");
            }
        }, () => _presetService?.CurrentPreset != null);

        CancelRenameCommand = new RelayCommand(() =>
        {
            // Restore original name and exit renaming mode
            PresetName = _originalPresetName;
            IsRenaming = false;
        }, () => IsRenaming);

        DeletePresetCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DeletePresetAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not delete preset");
                _toastService?.Error("Delete Failed", $"Could not delete preset: {e.Message}");
            }
        }, () => CanDeletePreset && _presetService?.CurrentPreset != null);

        DuplicatePresetCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DuplicatePresetAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not duplicate preset");
                _toastService?.Error("Duplicate Failed", $"Could not duplicate preset: {e.Message}");
            }
        }, () => _presetService?.CurrentPreset != null);

        ExportTomlCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ExportPresetAsync(ExportFormat.Toml);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not export preset");
                _toastService?.Error("Export Failed", $"Could not export preset: {e.Message}");
            }
        }, () => _presetService?.CurrentPreset != null);

        ExportIniCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ExportPresetAsync(ExportFormat.Ini);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not export preset");
                _toastService?.Error("Export Failed", $"Could not export preset: {e.Message}");
            }
        }, () => _presetService?.CurrentPreset != null);

        ImportCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ImportPresetAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not import preset");
                _toastService?.Error("Import Failed", $"Could not import preset: {e.Message}");
            }
        });

        // Subscribe to process state changes
        if (_frpcProcessService != null)
        {
            _frpcProcessService.ProcessStateChanged += OnProcessStateChanged;
            _frpcProcessService.LogLineReceived += OnLogLineReceived;
        }

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
        }

        // Update initial state
        UpdateFrpcStatus();
        UpdatePresetInfo();
        _ = RefreshServiceStatusAsync();

        // Timer to update runtime display
        _runtimeUpdateTimer = new System.Timers.Timer(1000);
        _runtimeUpdateTimer.Elapsed += (_, _) =>
        {
            if (IsFrpcRunning)
            {
                UpdateOverviewStats();
            }
        };
        _runtimeUpdateTimer.Start();
    }

    private readonly System.Timers.Timer _runtimeUpdateTimer;

    partial void OnIsFrpcRunningChanged(bool value)
    {
        StartFrpcCommand.NotifyCanExecuteChanged();
        StopFrpcCommand.NotifyCanExecuteChanged();
        RestartFrpcCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(NeedsServiceInstall));
        StatusMessage = value ? "Frpc is running" : "Frpc is not running";
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsServiceInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedsServiceInstall));
        StartFrpcCommand.NotifyCanExecuteChanged();
        InstallServiceCommand.NotifyCanExecuteChanged();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsNativeModeChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedsServiceInstall));
        InstallServiceCommand.NotifyCanExecuteChanged();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPresetNameChanged(string value)
    {
        RenamePresetCommand.NotifyCanExecuteChanged();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsRenamingChanged(bool value)
    {
        CancelRenameCommand.NotifyCanExecuteChanged();
    }

    private void OnProcessStateChanged(object? sender, ProcessStateChangedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsFrpcRunning = e.IsRunning;
            FrpcProcessId = e.ProcessId;
            _logger?.LogInformation("Frpc process state changed: IsRunning={IsRunning}, ProcessId={ProcessId}",
                e.IsRunning, e.ProcessId);
            UpdateOverviewStats();
        });
    }

    private void OnLogLineReceived(object? sender, LogLineEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LastLogLine = $"[{e.LogLevel}] {e.LogLine}";
        });
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        UpdatePresetInfo();
        _ = RefreshServiceStatusAsync();
    }

    private void UpdateFrpcStatus()
    {
        if (_frpcProcessService == null) return;
        IsFrpcRunning = _frpcProcessService.IsRunning;
        FrpcProcessId = _frpcProcessService.ProcessId;
        StatusMessage = IsFrpcRunning ? $"Frpc is running (PID: {FrpcProcessId})" : "Frpc is not running";
        UpdateOverviewStats();
    }

    public void RefreshPresetInfo()
    {
        UpdatePresetInfo();
    }

    private void UpdatePresetInfo()
    {
        if (_presetService?.CurrentPreset != null)
        {
            PresetName = _presetService.CurrentPreset.Name;
            ServerAddress = _presetService.CurrentPreset.Configuration.CommonConfig?.ServerAddr ?? "Not configured";
            ProxyCount = _presetService.CurrentPreset.Configuration.Proxies.Count;
            HasConfiguration = _presetService.CurrentPreset.Configuration.CommonConfig?.ServerAddr != null;
        }
        else
        {
            PresetName = "";
            ServerAddress = "Not configured";
            ProxyCount = 0;
            HasConfiguration = false;
        }

        CanDeletePreset = _presetService?.Presets.Count > 1;
        RenamePresetCommand.NotifyCanExecuteChanged();
        CancelRenameCommand.NotifyCanExecuteChanged();
        DeletePresetCommand.NotifyCanExecuteChanged();
        DuplicatePresetCommand.NotifyCanExecuteChanged();
        ExportTomlCommand.NotifyCanExecuteChanged();
        ExportIniCommand.NotifyCanExecuteChanged();
        UpdateOverviewStats();
    }

    private void UpdateOverviewStats()
    {
        // Running instance count (0 or 1 since we only support one instance)
        RunningInstanceCount = IsFrpcRunning ? 1 : 0;

        // Current preset runtime
        if (IsFrpcRunning && _frpcProcessService?.StartTime != null)
        {
            var runtime = DateTime.Now - _frpcProcessService.StartTime.Value;
            CurrentPresetRuntime = FormatRuntime(runtime);
        }
        else
        {
            CurrentPresetRuntime = _localizationService?.GetString("NotRunning") ?? "Not running";
        }

        // Total active proxies across all presets
        TotalActiveProxies = _presetService?.Presets.Sum(p => p.Configuration.Proxies.Count) ?? 0;
    }

    private async Task InstallServiceAsync()
    {
        if (_systemServiceManager == null || _configurationService == null || _presetService?.CurrentPreset == null)
            return;

        var settings = _presetService.CurrentPreset.Deployment;
        var binaryPath = settings.FrpcBinaryPath;
        if (string.IsNullOrEmpty(binaryPath))
        {
            _toastService?.Error("Install Failed", "No frpc binary path configured. Go to Deployment settings.");
            return;
        }

        var configPath = _configurationService.GetDefaultConfigPath();

        // Save config first
        await _configurationService.SaveConfigurationAsync(configPath, _presetService.CurrentPreset.Configuration);

        var serviceName = _systemServiceManager.GetDefaultServiceName();
        var scope = GetServiceScope();

        var config = new ServiceConfig
        {
            ServiceName = serviceName,
            BinaryPath = binaryPath,
            ConfigPath = configPath,
            Scope = scope,
            AutoStart = settings.AutoStartOnBoot
        };

        _logger?.LogInformation("Installing frpc service...");
        var success = await _systemServiceManager.InstallServiceAsync(config);
        if (success)
        {
            IsServiceInstalled = true;
            _toastService?.Success("Service Installed", "Frpc service has been installed");
            _logger?.LogInformation("Frpc service installed successfully");
        }
        else
        {
            _toastService?.Error("Install Failed", "Could not install frpc service");
        }
    }

    private async Task RefreshServiceStatusAsync()
    {
        if (_systemServiceManager == null || _presetService?.CurrentPreset == null) return;

        var settings = _presetService.CurrentPreset.Deployment;
        IsNativeMode = settings.DeploymentMode != "docker";

        if (!IsNativeMode) return;

        try
        {
            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var scope = GetServiceScope();
            var isInstalled = await _systemServiceManager.IsServiceInstalledAsync(serviceName);
            IsServiceInstalled = isInstalled;

            if (isInstalled)
            {
                var isRunning = await _systemServiceManager.IsServiceRunningAsync(serviceName, scope);
                IsServiceRunning = isRunning;
                switch (isRunning)
                {
                    case true when !IsFrpcRunning:
                        IsFrpcRunning = true;
                        StatusMessage = "Frpc service is running";
                        break;
                    case false when IsFrpcRunning && _frpcProcessService is { IsRunning: false }:
                        IsFrpcRunning = false;
                        StatusMessage = "Frpc is not running";
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to check service status");
        }
    }

    private ServiceScope GetServiceScope()
    {
        var settings = _presetService?.CurrentPreset?.Deployment;
        return settings?.ServiceScope == "system" ? ServiceScope.System : ServiceScope.User;
    }

    private static string FormatRuntime(TimeSpan runtime)
    {
        if (runtime.TotalDays >= 1)
            return $"{(int)runtime.TotalDays}d {runtime.Hours}h {runtime.Minutes}m";
        if (runtime.TotalHours >= 1)
            return $"{(int)runtime.TotalHours}h {runtime.Minutes}m {runtime.Seconds}s";
        return runtime.TotalMinutes >= 1 ? $"{(int)runtime.TotalMinutes}m {runtime.Seconds}s" : $"{runtime.Seconds}s";
    }

    private async Task StartFrpcAsync()
    {
        if (_configurationService == null) return;

        // Save current preset configuration to the default config path for frpc
        if (_presetService?.CurrentPreset != null)
        {
            var configPath = _configurationService.GetDefaultConfigPath();
            await _configurationService.SaveConfigurationAsync(configPath, _presetService.CurrentPreset.Configuration);
        }

        // Validate configuration before starting
        if (_validationService != null && _presetService?.CurrentPreset != null)
        {
            var validation = _validationService.ValidateConfiguration(_presetService.CurrentPreset.Configuration);
            if (!validation.IsValid)
            {
                var errors = string.Join(", ", validation.Errors);
                _logger?.LogError("Configuration validation failed: {Errors}", errors);
                _toastService?.Error("Configuration Error", $"Validation failed: {errors}");
                return;
            }

            if (validation.Warnings.Count > 0)
            {
                _logger?.LogWarning("Configuration validation warnings: {Warnings}",
                    string.Join(", ", validation.Warnings));
            }
        }

        // Native mode with service installed: start via service manager
        if (IsNativeMode && IsServiceInstalled && _systemServiceManager != null)
        {
            _logger?.LogInformation("Starting frpc service...");
            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var scope = GetServiceScope();
            var success = await _systemServiceManager.StartServiceAsync(serviceName, scope);
            if (success)
            {
                IsFrpcRunning = true;
                IsServiceRunning = true;
                _toastService?.Success("Frpc Started", "Frpc service is now running");
            }
            else
            {
                _toastService?.Error("Start Failed", "Could not start frpc service");
            }
            return;
        }

        // Fallback: start as direct process (docker mode or no service)
        if (_frpcProcessService == null) return;

        var configPathForStart = _configurationService.GetDefaultConfigPath();
        _logger?.LogInformation("Starting frpc...");
        var processSuccess = await _frpcProcessService.StartAsync(configPathForStart);
        if (processSuccess)
        {
            _toastService?.Success("Frpc Started", "Frpc client is now running");
        }
        else
        {
            _logger?.LogWarning("Failed to start frpc with config: {ConfigPath}", configPathForStart);
            _toastService?.Error("Start Failed", "Could not start frpc client");
        }
    }

    private async Task StopFrpcAsync()
    {
        // Native mode with service installed: stop via service manager
        if (IsNativeMode && IsServiceInstalled && _systemServiceManager != null)
        {
            _logger?.LogInformation("Stopping frpc service...");
            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var scope = GetServiceScope();
            var success = await _systemServiceManager.StopServiceAsync(serviceName, scope);
            if (success)
            {
                IsFrpcRunning = false;
                IsServiceRunning = false;
                _toastService?.Success("Frpc Stopped", "Frpc service has been stopped");
            }
            else
            {
                _toastService?.Error("Stop Failed", "Could not stop frpc service");
            }
            return;
        }

        // Fallback: stop direct process
        if (_frpcProcessService == null) return;
        _logger?.LogInformation("Stopping frpc...");
        await _frpcProcessService.StopAsync();
        _toastService?.Success("Frpc Stopped", "Frpc client has been stopped");
    }

    private async Task RestartFrpcAsync()
    {
        if (_frpcProcessService == null || _configurationService == null) return;
        _logger?.LogInformation("Restarting frpc...");

        // Save current preset configuration to the default config path for frpc
        if (_presetService?.CurrentPreset != null)
        {
            var configPath = _configurationService.GetDefaultConfigPath();
            await _configurationService.SaveConfigurationAsync(configPath, _presetService.CurrentPreset.Configuration);
        }

        var configPathForRestart = _configurationService.GetDefaultConfigPath();
        var success = await _frpcProcessService.RestartAsync(configPathForRestart);
        if (success)
        {
            _toastService?.Success("Frpc Restarted", "Frpc client has been restarted");
        }
        else
        {
            _logger?.LogWarning("Failed to restart frpc with config: {ConfigPath}", configPathForRestart);
            _toastService?.Error("Restart Failed", "Could not restart frpc client");
        }
    }

    private async Task RenamePresetAsync()
    {
        if (_presetService?.CurrentPreset == null) return;

        // Validate the new name
        if (string.IsNullOrWhiteSpace(PresetName))
        {
            // Restore original name if empty
            PresetName = _originalPresetName;
            IsRenaming = false;
            return;
        }

        // Only rename if name actually changed
        if (PresetName != _originalPresetName)
        {
            var oldName = _presetService.CurrentPreset.Name;
            await _presetService.RenamePresetAsync(_presetService.CurrentPreset.Id, PresetName);
            _toastService?.Success("Preset Renamed", $"Renamed preset from '{oldName}' to '{PresetName}'");
        }

        IsRenaming = false;
    }

    private async Task DeletePresetAsync()
    {
        if (_presetService?.CurrentPreset == null) return;
        if (_presetService.Presets.Count <= 1)
        {
            _toastService?.Warning("Cannot Delete", "Cannot delete the last preset");
            return;
        }

        var name = _presetService.CurrentPreset.Name;
        await _presetService.DeletePresetAsync(_presetService.CurrentPreset.Id);
        _toastService?.Success("Preset Deleted", $"Deleted preset: {name}");
    }

    private async Task DuplicatePresetAsync()
    {
        if (_presetService?.CurrentPreset == null) return;

        var originalName = _presetService.CurrentPreset.Name;
        var duplicate = await _presetService.DuplicatePresetAsync(_presetService.CurrentPreset.Id);
        _toastService?.Success("Preset Duplicated", $"Created copy of '{originalName}' as '{duplicate.Name}'");
    }

    private async Task ExportPresetAsync(ExportFormat format)
    {
        if (_presetService?.CurrentPreset == null) return;

        try
        {
            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
                return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider == null) return;

            var extension = format == ExportFormat.Toml ? "toml" : "ini";
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = L("ExportPreset"),
                SuggestedFileName = $"{_presetService.CurrentPreset.Name}.{extension}",
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType(format == ExportFormat.Toml ? "TOML Files" : "INI Files")
                    {
                        Patterns = [$"*.{extension}"]
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (file != null)
            {
                await _presetService.ExportPresetAsync(_presetService.CurrentPreset.Id, file.Path.LocalPath, format);
                _toastService?.Success("Export Complete", L("PresetExported", file.Name));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exporting preset");
            _toastService?.Error("Export Failed", L("ExportFailed"));
        }
    }

    private async Task ImportPresetAsync()
    {
        try
        {
            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
                return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = L("ImportPreset"),
                FileTypeFilter =
                [
                    new FilePickerFileType("Configuration Files")
                    {
                        Patterns = ["*.toml", "*.ini"]
                    },
                    new FilePickerFileType("TOML Files")
                    {
                        Patterns = ["*.toml"]
                    },
                    new FilePickerFileType("INI Files")
                    {
                        Patterns = ["*.ini"]
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = ["*"]
                    }
                ],
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                var format = extension == ".ini" ? ExportFormat.Ini : ExportFormat.Toml;

                var preset = await _presetService!.ImportPresetAsync(file.Path.LocalPath, format);
                await _presetService.SwitchPresetAsync(preset.Id);

                _toastService?.Success("Import Complete", L("PresetImported", preset.Name));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error importing preset");
            _toastService?.Error("Import Failed", L("ImportFailed"));
        }
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;
}
