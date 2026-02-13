using System.Runtime.InteropServices;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for deployment management
/// </summary>
public partial class DeploymentViewModel : ObservableObject
{
    private readonly ILogger<DeploymentViewModel>? _logger;
    private readonly IFrpcDownloader? _frpcDownloader;
    private readonly IFrpcVersionService? _frpcVersionService;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IDockerDeploymentService? _dockerDeploymentService;
    private readonly ISystemServiceManager? _systemServiceManager;
    private readonly IProcessManager? _processManager;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;

    #region Mode Selection

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNativeMode))]
    [NotifyPropertyChangedFor(nameof(IsDockerMode))]
    private string _selectedDeploymentMode = "native";

    public bool IsNativeMode => SelectedDeploymentMode == "native";
    public bool IsDockerMode => SelectedDeploymentMode == "docker";

    #endregion

    #region Native - Service Configuration

    // Frpc Path & Version
    [ObservableProperty] private string _frpcBinaryPath = "";
    [ObservableProperty] private bool _isPathValid;
    [ObservableProperty] private string? _detectedVersion;
    [ObservableProperty] private bool _isCheckingPath;

    // Service Settings
    [ObservableProperty] private string _serviceScopeValue = "user";
    [ObservableProperty] private bool _autoStartOnBoot = true;
    [ObservableProperty] private bool _serviceEnabled = true;
    [ObservableProperty] private bool _isServiceInstalled;
    [ObservableProperty] private bool _isServiceRunning;
    [ObservableProperty] private bool _isServiceChecking;
    [ObservableProperty] private ServiceStatus? _serviceStatus;

    private Core.Interfaces.ServiceScope GetServiceScopeEnum() =>
        ServiceScopeValue == "system" ? Core.Interfaces.ServiceScope.System : Core.Interfaces.ServiceScope.User;

    #endregion

    #region Docker Properties

    [ObservableProperty] private bool _isDockerAvailable;
    [ObservableProperty] private bool _isDockerChecking;
    [ObservableProperty] private string _dockerContainerName = "frapa-clonia-frpc";
    [ObservableProperty] private string _dockerImageName = "fatedier/frpc:latest";
    [ObservableProperty] private string _dockerImageTag = "latest";
    [ObservableProperty] private string _dockerComposePath = "";
    [ObservableProperty] private bool _isContainerRunning;

    #endregion

    public IRelayCommand CheckFrpcPathCommand { get; }
    public IRelayCommand ConfigureFrpcCommand { get; }
    public IRelayCommand RefreshServiceStatusCommand { get; }
    public IRelayCommand InstallServiceCommand { get; }
    public IRelayCommand UninstallServiceCommand { get; }
    public IRelayCommand StartServiceCommand { get; }
    public IRelayCommand StopServiceCommand { get; }
    public IRelayCommand CheckDockerCommand { get; }
    public IRelayCommand GenerateDockerComposeCommand { get; }
    public IRelayCommand StartDockerCommand { get; }
    public IRelayCommand StopDockerCommand { get; }

    public List<string> DeploymentModes { get; } = ["native", "docker"];
    public List<string> ServiceScopes { get; } = ["user", "system"];

    // Default constructor for design-time support
    public DeploymentViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DeploymentViewModel>.Instance,
        null!, null!, null!, null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public DeploymentViewModel(
        ILogger<DeploymentViewModel> logger,
        IFrpcDownloader frpcDownloader,
        IFrpcVersionService frpcVersionService,
        INativeDeploymentService nativeDeploymentService,
        IDockerDeploymentService dockerDeploymentService,
        ISystemServiceManager systemServiceManager,
        IProcessManager processManager,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        ILocalizationService? localizationService,
        MainWindowViewModel? mainWindowViewModel)
    {
        _logger = logger;
        _frpcDownloader = frpcDownloader;
        _frpcVersionService = frpcVersionService;
        _nativeDeploymentService = nativeDeploymentService;
        _dockerDeploymentService = dockerDeploymentService;
        _systemServiceManager = systemServiceManager;
        _processManager = processManager;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _localizationService = localizationService;

        CheckFrpcPathCommand = CreateAsyncCommand(CheckFrpcPathAsync, "Error checking frpc path");
        ConfigureFrpcCommand = CreateAsyncCommand(ConfigureFrpcAsync, "Error opening configuration");
        RefreshServiceStatusCommand = CreateAsyncCommand(RefreshServiceStatusAsync, "Error refreshing service status");
        InstallServiceCommand = CreateAsyncCommand(InstallServiceAsync, "Error installing service");
        UninstallServiceCommand = CreateAsyncCommand(UninstallServiceAsync, "Error uninstalling service");
        StartServiceCommand = CreateAsyncCommand(StartServiceAsync, "Error starting service");
        StopServiceCommand = CreateAsyncCommand(StopServiceAsync, "Error stopping service");
        CheckDockerCommand = CreateAsyncCommand(CheckDockerAsync, "Error checking Docker availability");
        GenerateDockerComposeCommand = CreateAsyncCommand(GenerateDockerComposeAsync, "Error generating docker compose");
        StartDockerCommand = CreateAsyncCommand(StartDockerAsync, "Error starting docker");
        StopDockerCommand = CreateAsyncCommand(StopDockerAsync, "Error stopping docker");
    }

    private IRelayCommand CreateAsyncCommand(Func<Task> action, string errorMessage)
    {
        return new RelayCommand(async void () =>
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "{Message}", errorMessage);
            }
        });
    }

    public void Initialize()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Auto-detect frpc path
        await AutoDetectFrpcPathAsync();
        // Refresh service status
        await RefreshServiceStatusAsync();
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    partial void OnSelectedDeploymentModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsNativeMode));
        OnPropertyChanged(nameof(IsDockerMode));
    }

    #region Native Methods

    private async Task AutoDetectFrpcPathAsync()
    {
        try
        {
            IsCheckingPath = true;
            _logger?.LogInformation("Auto-detecting frpc path");

            // First check if we have a saved path
            var savedPath = FrpcBinaryPath;
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                await ValidateFrpcPathAsync(savedPath);
                return;
            }

            // Try PATH
            var whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var result = await _processManager!.ExecuteAsync(whichCmd, "frpc");

            if (result.Success)
            {
                var path = result.StandardOutput.Split('\n').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    FrpcBinaryPath = path;
                    await ValidateFrpcPathAsync(path);
                    return;
                }
            }

            // Check common locations
            var commonPaths = GetCommonBinaryPaths();
            foreach (var testPath in commonPaths)
            {
                if (File.Exists(testPath))
                {
                    FrpcBinaryPath = testPath;
                    await ValidateFrpcPathAsync(testPath);
                    return;
                }
            }

            // Not found
            IsPathValid = false;
            DetectedVersion = null;
            _logger?.LogInformation("Frpc not found in PATH or common locations");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error auto-detecting frpc path");
            IsPathValid = false;
            DetectedVersion = null;
        }
        finally
        {
            IsCheckingPath = false;
        }
    }

    private async Task CheckFrpcPathAsync()
    {
        await ValidateFrpcPathAsync(FrpcBinaryPath);
    }

    private async Task ValidateFrpcPathAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            IsPathValid = false;
            DetectedVersion = null;
            return;
        }

        try
        {
            IsCheckingPath = true;
            var version = await _frpcVersionService!.GetBinaryVersionAsync(path);
            if (version != null)
            {
                IsPathValid = true;
                DetectedVersion = version.Version;
                _logger?.LogInformation("Frpc validated: {Path} v{Version}", path, DetectedVersion);
            }
            else
            {
                IsPathValid = false;
                DetectedVersion = null;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating frpc path");
            IsPathValid = false;
            DetectedVersion = null;
        }
        finally
        {
            IsCheckingPath = false;
        }
    }

    private async Task ConfigureFrpcAsync()
    {
        if (_serviceProvider == null) return;

        try
        {
            var viewModel = new FrpcConfigurationViewModel(
                _serviceProvider.GetRequiredService<ILogger<FrpcConfigurationViewModel>>(),
                _serviceProvider.GetRequiredService<IFrpcVersionService>(),
                _serviceProvider.GetRequiredService<IFrpcDownloader>(),
                _serviceProvider.GetRequiredService<INativeDeploymentService>(),
                _serviceProvider.GetRequiredService<IPackageManagerService>(),
                _serviceProvider.GetRequiredService<IProcessManager>(),
                _serviceProvider,
                _toastService,
                _localizationService);

            await viewModel.InitializeAsync(FrpcBinaryPath);

            var dialog = new FrpcConfigurationDialog(viewModel);

            // Get the main window
            var mainWindow = _serviceProvider.GetService<Window>();
            if (mainWindow != null)
            {
                await dialog.ShowDialog(mainWindow);
            }
            else
            {
                dialog.Show();
            }

            if (viewModel.DialogResult && !string.IsNullOrEmpty(viewModel.FrpcBinaryPath))
            {
                FrpcBinaryPath = viewModel.FrpcBinaryPath;
                await ValidateFrpcPathAsync(FrpcBinaryPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening frpc configuration dialog");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotOpenConfiguration"));
        }
    }

    #endregion

    #region Service Methods

    private async Task RefreshServiceStatusAsync()
    {
        try
        {
            IsServiceChecking = true;

            if (_systemServiceManager != null)
            {
                var serviceName = _systemServiceManager.GetDefaultServiceName();
                ServiceStatus = await _systemServiceManager.GetServiceStatusAsync(
                    serviceName,
                    GetServiceScopeEnum());

                IsServiceInstalled = ServiceStatus.IsInstalled;
                IsServiceRunning = ServiceStatus.IsRunning;
                AutoStartOnBoot = ServiceStatus.IsAutoStartEnabled;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing service status");
        }
        finally
        {
            IsServiceChecking = false;
        }
    }

    private async Task InstallServiceAsync()
    {
        try
        {
            if (_systemServiceManager == null)
            {
                _toastService?.Warning(L("Toast_NotAvailable"), L("Toast_ServiceManagerNotAvailable"));
                return;
            }

            if (string.IsNullOrEmpty(FrpcBinaryPath) || !IsPathValid)
            {
                _toastService?.Warning(L("Toast_NoBinary"), L("Toast_ConfigureFrpcFirst"));
                return;
            }

            var configPath = _serviceProvider?.GetRequiredService<IConfigurationService>()?.GetDefaultConfigPath();
            if (string.IsNullOrEmpty(configPath))
            {
                _toastService?.Warning(L("Toast_NoConfig"), L("Toast_CreateConfigFirst"));
                return;
            }

            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var config = new ServiceConfig
            {
                ServiceName = serviceName,
                BinaryPath = FrpcBinaryPath,
                ConfigPath = configPath,
                Scope = GetServiceScopeEnum(),
                AutoStart = AutoStartOnBoot
            };

            var success = await _systemServiceManager.InstallServiceAsync(config);
            if (success)
            {
                IsServiceInstalled = true;
                _toastService?.Success(L("Toast_ServiceInstalled"), L("Toast_FrpcServiceInstalled"));
                await RefreshServiceStatusAsync();
            }
            else
            {
                _toastService?.Error(L("Toast_InstallFailed"), L("Toast_CouldNotInstallService"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error installing service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceInstallFailed", ex.Message));
        }
    }

    private async Task UninstallServiceAsync()
    {
        try
        {
            if (_systemServiceManager == null) return;

            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var success = await _systemServiceManager.UninstallServiceAsync(serviceName);

            if (success)
            {
                IsServiceInstalled = false;
                IsServiceRunning = false;
                _toastService?.Success(L("Toast_ServiceUninstalled"), L("Toast_FrpcServiceUninstalled"));
            }
            else
            {
                _toastService?.Error(L("Toast_UninstallFailed"), L("Toast_CouldNotUninstallService"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error uninstalling service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceUninstallFailed", ex.Message));
        }
    }

    private async Task StartServiceAsync()
    {
        try
        {
            if (_systemServiceManager == null) return;

            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var scope = GetServiceScopeEnum();
            var success = await _systemServiceManager.StartServiceAsync(serviceName, scope);

            if (success)
            {
                IsServiceRunning = true;
                _toastService?.Success(L("Toast_ServiceStarted"), L("Toast_FrpcServiceStarted"));
            }
            else
            {
                _toastService?.Error(L("Toast_StartFailed"), L("Toast_CouldNotStartService"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceStartFailed", ex.Message));
        }
    }

    private async Task StopServiceAsync()
    {
        try
        {
            if (_systemServiceManager == null) return;

            var serviceName = _systemServiceManager.GetDefaultServiceName();
            var scope = GetServiceScopeEnum();
            var success = await _systemServiceManager.StopServiceAsync(serviceName, scope);

            if (success)
            {
                IsServiceRunning = false;
                _toastService?.Success(L("Toast_ServiceStopped"), L("Toast_FrpcServiceStopped"));
            }
            else
            {
                _toastService?.Error(L("Toast_StopFailed"), L("Toast_CouldNotStopService"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceStopFailed", ex.Message));
        }
    }

    #endregion

    #region Docker Methods

    private async Task CheckDockerAsync()
    {
        try
        {
            IsDockerChecking = true;

            if (_dockerDeploymentService != null)
                IsDockerAvailable = await _dockerDeploymentService.IsDockerAvailableAsync();

            if (IsDockerAvailable)
            {
                _toastService?.Success(L("Toast_DockerAvailable"), L("Toast_DockerReady"));
            }
            else
            {
                _toastService?.Warning(L("Toast_DockerNotAvailable"), L("Toast_DockerNotInstalled"));
            }

            _logger?.LogInformation("Docker availability check: {IsAvailable}", IsDockerAvailable);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking Docker availability");
            IsDockerAvailable = false;
            _toastService?.Error(L("Toast_CheckFailed"), L("Toast_CouldNotCheckDocker"));
        }
        finally
        {
            IsDockerChecking = false;
        }
    }

    private async Task GenerateDockerComposeAsync()
    {
        try
        {
            if (_serviceProvider != null)
            {
                var configPath = _serviceProvider.GetRequiredService<IConfigurationService>().GetDefaultConfigPath();
                var config = new FrpcDockerConfig
                {
                    ImageName = "fatedier/frpc",
                    Tag = DockerImageTag,
                    ConfigPath = configPath,
                    ContainerName = DockerContainerName,
                    AutoRestart = true
                };

                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var outputPath = Path.Combine(downloadsPath, "frapa-clonia-docker");
                Directory.CreateDirectory(outputPath);

                if (_dockerDeploymentService != null)
                {
                    var composePath = await _dockerDeploymentService.GenerateDockerComposeAsync(outputPath, config);
                    DockerComposePath = composePath;

                    _toastService?.Success(L("Toast_Generated"), L("Toast_DockerComposeSaved", composePath));
                    _logger?.LogInformation("Generated docker-compose.yml at {Path}", composePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating docker-compose.yml");
            _toastService?.Error(L("Toast_GenerationFailed"), L("Toast_CouldNotGenerateDockerCompose"));
        }
    }

    private async Task StartDockerAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DockerComposePath))
            {
                _toastService?.Warning(L("Toast_NoConfiguration"), L("Toast_GenerateDockerComposeFirst"));
                return;
            }

            var composeDirectory = Path.GetDirectoryName(DockerComposePath);
            if (composeDirectory == null)
            {
                _toastService?.Error(L("Toast_InvalidPath"), L("Toast_CouldNotDetermineDirectory"));
                return;
            }

            var success = _dockerDeploymentService != null &&
                          await _dockerDeploymentService.StartDockerComposeAsync(composeDirectory);
            if (success)
            {
                _toastService?.Success(L("Toast_ContainerStarted"), L("Toast_DockerContainerRunning"));
                if (_dockerDeploymentService != null)
                    IsContainerRunning = await _dockerDeploymentService.IsContainerRunningAsync(DockerContainerName);
            }
            else
            {
                _toastService?.Error(L("Toast_StartFailed"), L("Toast_CouldNotStartContainer"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting Docker container");
            _toastService?.Error(L("Toast_Error"), L("Toast_FailedToStartContainer", ex.Message));
        }
    }

    private async Task StopDockerAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(DockerComposePath))
            {
                _toastService?.Warning(L("Toast_NoConfiguration"), L("Toast_NoDockerComposeFound"));
                return;
            }

            var composeDirectory = Path.GetDirectoryName(DockerComposePath);
            if (composeDirectory == null)
            {
                _toastService?.Error(L("Toast_InvalidPath"), L("Toast_CouldNotDetermineDirectory"));
                return;
            }

            var success = _dockerDeploymentService != null &&
                          await _dockerDeploymentService.StopDockerComposeAsync(composeDirectory);
            if (success)
            {
                _toastService?.Success(L("Toast_ContainerStopped"), L("Toast_DockerContainerStopped"));
                IsContainerRunning = false;
            }
            else
            {
                _toastService?.Error(L("Toast_StopFailed"), L("Toast_CouldNotStopContainer"));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Docker container");
            _toastService?.Error(L("Toast_Error"), L("Toast_FailedToStopContainer", ex.Message));
        }
    }

    #endregion

    #region Settings Sync

    public void LoadFromPreset(ConfigPreset preset)
    {
        var settings = preset.Deployment;

        SelectedDeploymentMode = settings.DeploymentMode;
        FrpcBinaryPath = settings.FrpcBinaryPath ?? "";

        DockerContainerName = settings.DockerContainerName;
        DockerImageName = settings.DockerImageName;
        DockerImageTag = settings.DockerImageTag;

        ServiceScopeValue = settings.ServiceScope;
        AutoStartOnBoot = settings.AutoStartOnBoot;
        ServiceEnabled = settings.ServiceEnabled;
    }

    public void SaveToPreset(ConfigPreset preset)
    {
        var settings = preset.Deployment;

        settings.DeploymentMode = SelectedDeploymentMode;
        settings.FrpcBinaryPath = FrpcBinaryPath;

        settings.DockerContainerName = DockerContainerName;
        settings.DockerImageName = DockerImageName;
        settings.DockerImageTag = DockerImageTag;

        settings.ServiceScope = ServiceScopeValue;
        settings.AutoStartOnBoot = AutoStartOnBoot;
        settings.ServiceEnabled = ServiceEnabled;
    }

    #endregion

    private static IEnumerable<string> GetCommonBinaryPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return
            [
                @"C:\Program Files\frpc\frpc.exe",
                @"C:\ProgramData\chocolatey\bin\frpc.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims", "frpc.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links", "frpc.exe")
            ];
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return
            [
                "/usr/local/bin/frpc",
                "/opt/homebrew/bin/frpc",
                "/usr/bin/frpc"
            ];
        }

        // Linux
        return
        [
            "/usr/local/bin/frpc",
            "/usr/bin/frpc",
            "/opt/frpc/frpc"
        ];
    }
}
