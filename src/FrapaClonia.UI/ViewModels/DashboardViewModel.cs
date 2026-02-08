using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;

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

    [ObservableProperty] private bool _isFrpcRunning;

    [ObservableProperty] private int? _frpcProcessId;

    [ObservableProperty] private string _statusMessage = "Frpc is not running";

    [ObservableProperty] private string _lastLogLine = "";

    [ObservableProperty] private bool _hasConfiguration;

    [ObservableProperty] private string _serverAddress = "Not configured";

    [ObservableProperty] private int _proxyCount;

    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxyListCommand { get; }
    public IRelayCommand NavigateToDeploymentCommand { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IRelayCommand NavigateToSettingsCommand { get; }
    public IRelayCommand NavigateToLogsCommand { get; }
    public IRelayCommand StartFrpcCommand { get; }
    public IRelayCommand StopFrpcCommand { get; }
    public IRelayCommand RestartFrpcCommand { get; }

    // Default constructor for design-time support
    public DashboardViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DashboardViewModel>.Instance,
        null!,
        null!,
        null!)
    {
    }

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IFrpcProcessService frpcProcessService,
        IConfigurationService configurationService,
        IValidationService validationService)
    {
        _logger = logger;
        _frpcProcessService = frpcProcessService;
        _configurationService = configurationService;
        _validationService = validationService;

        // For design-time or when services are null, use empty commands

        NavigateToServerConfigCommand = new RelayCommand(() => _logger.LogInformation("Navigate to Server Config"));
        NavigateToProxyListCommand = new RelayCommand(() => _logger.LogInformation("Navigate to Proxy List"));
        NavigateToDeploymentCommand = new RelayCommand(() => _logger.LogInformation("Navigate to Deployment"));
        NavigateToSettingsCommand = new RelayCommand(() => _logger.LogInformation("Navigate to Settings"));
        NavigateToLogsCommand = new RelayCommand(() => _logger.LogInformation("Navigate to Logs"));

        StartFrpcCommand = new RelayCommand(async void () =>
        {
            try
            {
                await StartFrpcAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not start frpc");
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
            }
        }, () => IsFrpcRunning);

        // Subscribe to process state changes
        _frpcProcessService.ProcessStateChanged += OnProcessStateChanged;
        _frpcProcessService.LogLineReceived += OnLogLineReceived;

        // Update initial state
        UpdateFrpcStatus();
    }

    partial void OnIsFrpcRunningChanged(bool value)
    {
        StartFrpcCommand.NotifyCanExecuteChanged();
        StopFrpcCommand.NotifyCanExecuteChanged();
        RestartFrpcCommand.NotifyCanExecuteChanged();
        StatusMessage = value ? "Frpc is running" : "Frpc is not running";
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

    private void UpdateFrpcStatus()
    {
        if (_frpcProcessService == null) return;
        IsFrpcRunning = _frpcProcessService.IsRunning;
        FrpcProcessId = _frpcProcessService.ProcessId;
        StatusMessage = IsFrpcRunning ? $"Frpc is running (PID: {FrpcProcessId})" : "Frpc is not running";
    }

    private async Task StartFrpcAsync()
    {
        if (_frpcProcessService == null || _configurationService == null) return;

        var configPath = _configurationService.GetDefaultConfigPath();

        // Validate configuration before starting
        if (_validationService != null)
        {
            var config = await _configurationService.LoadConfigurationAsync(configPath);
            if (config != null)
            {
                var validation = _validationService.ValidateConfiguration(config);
                if (!validation.IsValid)
                {
                    _logger?.LogError("Configuration validation failed: {Errors}",
                        string.Join(", ", validation.Errors));
                    return;
                }

                if (validation.Warnings.Count > 0)
                {
                    _logger?.LogWarning("Configuration validation warnings: {Warnings}",
                        string.Join(", ", validation.Warnings));
                }
            }
        }

        _logger?.LogInformation("Starting frpc...");
        var success = await _frpcProcessService.StartAsync(configPath);
        if (!success)
        {
            _logger?.LogWarning("Failed to start frpc with config: {ConfigPath}", configPath);
        }
    }

    private async Task StopFrpcAsync()
    {
        if (_frpcProcessService == null) return;
        _logger?.LogInformation("Stopping frpc...");
        await _frpcProcessService.StopAsync();
    }

    private async Task RestartFrpcAsync()
    {
        if (_frpcProcessService == null || _configurationService == null) return;
        _logger?.LogInformation("Restarting frpc...");
        var configPath = _configurationService.GetDefaultConfigPath();
        var success = await _frpcProcessService.RestartAsync(configPath);
        if (!success)
        {
            _logger?.LogWarning("Failed to restart frpc with config: {ConfigPath}", configPath);
        }
    }
}