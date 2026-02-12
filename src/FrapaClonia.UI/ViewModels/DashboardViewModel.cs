using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
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

    [ObservableProperty] private bool _isFrpcRunning;

    [ObservableProperty] private int? _frpcProcessId;

    [ObservableProperty] private string _statusMessage = "Frpc is not running";

    [ObservableProperty] private string _lastLogLine = "";

    [ObservableProperty] private bool _hasConfiguration;

    [ObservableProperty] private string _serverAddress = "Not configured";

    [ObservableProperty] private int _proxyCount;

    // Preset management properties
    [ObservableProperty] private string _presetName = "";

    [ObservableProperty] private bool _isRenaming;

    [ObservableProperty] private bool _canDeletePreset = true;

    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxyListCommand { get; }
    public IRelayCommand NavigateToDeploymentCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IRelayCommand NavigateToSettingsCommand { get; }
    public IRelayCommand NavigateToLogsCommand { get; }
    public IRelayCommand StartFrpcCommand { get; }
    public IRelayCommand StopFrpcCommand { get; }
    public IRelayCommand RestartFrpcCommand { get; }

    // Preset management commands
    public IRelayCommand RenamePresetCommand { get; }
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
        ILocalizationService? localizationService)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _configurationService = configurationService;
        _validationService = validationService;
        _presetService = presetService;
        _toastService = toastService;
        _localizationService = localizationService;

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

        // Preset management commands
        RenamePresetCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RenamePresetAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not rename preset");
                _toastService?.Error("Rename Failed", $"Could not rename preset: {e.Message}");
            }
        }, () => !string.IsNullOrWhiteSpace(PresetName) && _presetService?.CurrentPreset != null);

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
        }, () => CanDeletePreset);

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
    }

    partial void OnIsFrpcRunningChanged(bool value)
    {
        StartFrpcCommand.NotifyCanExecuteChanged();
        StopFrpcCommand.NotifyCanExecuteChanged();
        RestartFrpcCommand.NotifyCanExecuteChanged();
        StatusMessage = value ? "Frpc is running" : "Frpc is not running";
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPresetNameChanged(string value)
    {
        RenamePresetCommand.NotifyCanExecuteChanged();
    }

    private void OnProcessStateChanged(object? sender, ProcessStateChangedEventArgs e)
    {
        IsFrpcRunning = e.IsRunning;
        FrpcProcessId = e.ProcessId;
        _logger?.LogInformation("Frpc process state changed: IsRunning={IsRunning}, ProcessId={ProcessId}",
            e.IsRunning, e.ProcessId);
    }

    private void OnLogLineReceived(object? sender, LogLineEventArgs e)
    {
        LastLogLine = $"[{e.LogLevel}] {e.LogLine}";
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        UpdatePresetInfo();
    }

    private void UpdateFrpcStatus()
    {
        if (_frpcProcessService == null) return;
        IsFrpcRunning = _frpcProcessService.IsRunning;
        FrpcProcessId = _frpcProcessService.ProcessId;
        StatusMessage = IsFrpcRunning ? $"Frpc is running (PID: {FrpcProcessId})" : "Frpc is not running";
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
        DeletePresetCommand.NotifyCanExecuteChanged();
        DuplicatePresetCommand.NotifyCanExecuteChanged();
        ExportTomlCommand.NotifyCanExecuteChanged();
        ExportIniCommand.NotifyCanExecuteChanged();
    }

    private async Task StartFrpcAsync()
    {
        if (_frpcProcessService == null || _configurationService == null) return;

        // Save current preset configuration to the default config path for frpc
        if (_presetService?.CurrentPreset != null)
        {
            var configPath = _configurationService.GetDefaultConfigPath();
            await _configurationService.SaveConfigurationAsync(configPath, _presetService.CurrentPreset.Configuration);
        }

        var configPathForStart = _configurationService.GetDefaultConfigPath();

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

        _logger?.LogInformation("Starting frpc...");
        var success = await _frpcProcessService.StartAsync(configPathForStart);
        if (success)
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
        if (_presetService?.CurrentPreset == null || string.IsNullOrWhiteSpace(PresetName)) return;

        var oldName = _presetService.CurrentPreset.Name;
        await _presetService.RenamePresetAsync(_presetService.CurrentPreset.Id, PresetName);
        _toastService?.Success("Preset Renamed", $"Renamed preset from '{oldName}' to '{PresetName}'");
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
