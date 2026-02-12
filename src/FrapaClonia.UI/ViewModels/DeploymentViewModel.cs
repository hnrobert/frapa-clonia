using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for deployment management
/// </summary>
public partial class DeploymentViewModel : ObservableObject
{
    private readonly ILogger<DeploymentViewModel>? _logger;

    // ReSharper disable once NotAccessedField.Local
    private readonly IFrpcDownloader? _frpcDownloader;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IDockerDeploymentService? _dockerDeploymentService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;

    [ObservableProperty] private string _selectedDeploymentMode = "native";

    [ObservableProperty] private bool _isDockerAvailable;

    [ObservableProperty] private bool _isDockerChecking;

    [ObservableProperty] private bool _isNativeDeployed;

    [ObservableProperty] private bool _isNativeChecking;

    [ObservableProperty] private string? _deployedBinaryPath;

    [ObservableProperty] private string _dockerContainerName = "frapa-clonia-frpc";

    [ObservableProperty] private string _dockerImageName = "fatedier/frpc:latest";

    [ObservableProperty] private string _dockerComposePath = "";

    [ObservableProperty] private bool _isContainerRunning;

    public IRelayCommand CheckDockerCommand { get; }
    public IRelayCommand GenerateDockerComposeCommand { get; }
    public IRelayCommand StartDockerCommand { get; }
    public IRelayCommand StopDockerCommand { get; }
    public IRelayCommand CheckNativeCommand { get; }
    public IRelayCommand DeployNativeCommand { get; }
    public IRelayCommand DownloadFrpcCommand { get; }

    public List<string> DeploymentModes { get; } = ["native", "docker"];

    // Default constructor for design-time support
    public DeploymentViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DeploymentViewModel>.Instance,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!)
    {
    }

    public DeploymentViewModel(
        ILogger<DeploymentViewModel> logger,
        IFrpcDownloader frpcDownloader,
        INativeDeploymentService nativeDeploymentService,
        IDockerDeploymentService dockerDeploymentService,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        ILocalizationService? localizationService)
    {
        _logger = logger;
        _frpcDownloader = frpcDownloader;
        _nativeDeploymentService = nativeDeploymentService;
        _dockerDeploymentService = dockerDeploymentService;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _localizationService = localizationService;

        CheckDockerCommand = new RelayCommand(async void () =>
        {
            try
            {
                await CheckDockerAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error checking Docker availability");
            }
        });
        GenerateDockerComposeCommand = new RelayCommand(async void () =>
        {
            try
            {
                await GenerateDockerComposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error generating docker compose");
            }
        });
        StartDockerCommand = new RelayCommand(async void () =>
        {
            try
            {
                await StartDockerAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error starting docker");
            }
        });
        StopDockerCommand = new RelayCommand(async void () =>
        {
            try
            {
                await StopDockerAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error stopping docker");
            }
        });
        CheckNativeCommand = new RelayCommand(async void () =>
        {
            try
            {
                await CheckNativeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error checking docker native");
            }
        });
        DeployNativeCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DeployNativeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deploying native");
            }
        });
        DownloadFrpcCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DownloadFrpcAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error downloading frpc");
            }
        });

        // Note: Initialization is triggered by the View's OnLoaded event
    }

    public void Initialize()
    {
        _ = CheckNativeAsync();
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

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
                    Tag = "latest",
                    ConfigPath = configPath,
                    ContainerName = DockerContainerName,
                    AutoRestart = true
                };

                // Use user's Downloads directory for docker-compose
                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
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

    private async Task CheckNativeAsync()
    {
        try
        {
            IsNativeChecking = true;
            _logger?.LogInformation("CheckNativeAsync: Starting, IsNativeChecking={IsNativeChecking}", IsNativeChecking);

            if (_nativeDeploymentService != null)
            {
                IsNativeDeployed = await _nativeDeploymentService.IsDeployedAsync();
                if (IsNativeDeployed)
                {
                    DeployedBinaryPath = await _nativeDeploymentService.GetDeployedBinaryPathAsync();
                    _toastService?.Success(L("Toast_FrpcFound"), L("Toast_BinaryLocated", DeployedBinaryPath ?? ""));
                }
                else
                {
                    DeployedBinaryPath = null;
                    _toastService?.Info(L("Toast_FrpcNotFound"), L("Toast_DownloadAndDeploy"));
                }
            }
            else
            {
                _logger?.LogWarning("NativeDeploymentService is null");
            }

            _logger?.LogInformation("Native deployment check: IsDeployed={IsDeployed}, Path={Path}",
                IsNativeDeployed, DeployedBinaryPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking native deployment");
            IsNativeDeployed = false;
            _toastService?.Error(L("Toast_CheckFailed"), L("Toast_CouldNotCheckNative"));
        }
        finally
        {
            IsNativeChecking = false;
            _logger?.LogInformation("CheckNativeAsync: Completed, IsNativeChecking={IsNativeChecking}", IsNativeChecking);
        }
    }

    private Task DeployNativeAsync()
    {
        try
        {
            // This would typically involve:
            // 1. Downloading frpc if not already downloaded
            // 2. Extracting the archive
            // 3. Setting executable permissions
            // For now, this is a placeholder

            _toastService?.Warning(L("Toast_NotImplemented"), L("Toast_NativeDeploymentNotImplemented"));
            _logger?.LogWarning("Native deployment not yet implemented");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deploying native frpc");
            _toastService?.Error(L("Toast_DeploymentFailed"), L("Toast_CouldNotDeployNative"));
        }

        return Task.CompletedTask;
    }

    private Task DownloadFrpcAsync()
    {
        try
        {
            // Open the GitHub releases page in a browser
            var url = "https://github.com/fatedier/frpc/releases";
            _toastService?.Info(L("Toast_Download"), L("Toast_DownloadFrom", url));

            _logger?.LogInformation("Opened frpc download page: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening frpc download page");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotOpenDownloadPage"));
        }

        return Task.CompletedTask;
    }
}
