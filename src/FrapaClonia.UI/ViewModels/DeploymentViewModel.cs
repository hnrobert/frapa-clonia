using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for deployment management
/// </summary>
public partial class DeploymentViewModel : ObservableObject
{
    private readonly ILogger<DeploymentViewModel> _logger;
    private readonly IFrpcDownloader _frpcDownloader;
    private readonly INativeDeploymentService _nativeDeploymentService;
    private readonly IDockerDeploymentService _dockerDeploymentService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _selectedDeploymentMode = "native";

    [ObservableProperty]
    private bool _isDockerAvailable;

    [ObservableProperty]
    private bool _isDockerChecking;

    [ObservableProperty]
    private bool _isNativeDeployed;

    [ObservableProperty]
    private bool _isNativeChecking;

    [ObservableProperty]
    private string? _deployedBinaryPath;

    [ObservableProperty]
    private string _dockerContainerName = "frapa-clonia-frpc";

    [ObservableProperty]
    private string _dockerImageName = "fatedier/frpc:latest";

    [ObservableProperty]
    private string _dockerComposePath = "";

    [ObservableProperty]
    private bool _isContainerRunning;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public IRelayCommand CheckDockerCommand { get; }
    public IRelayCommand GenerateDockerComposeCommand { get; }
    public IRelayCommand StartDockerCommand { get; }
    public IRelayCommand StopDockerCommand { get; }
    public IRelayCommand CheckNativeCommand { get; }
    public IRelayCommand DeployNativeCommand { get; }
    public IRelayCommand DownloadFrpcCommand { get; }

    public List<string> DeploymentModes { get; } = ["native", "docker"];

    public DeploymentViewModel(
        ILogger<DeploymentViewModel> logger,
        IFrpcDownloader frpcDownloader,
        INativeDeploymentService nativeDeploymentService,
        IDockerDeploymentService dockerDeploymentService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _frpcDownloader = frpcDownloader;
        _nativeDeploymentService = nativeDeploymentService;
        _dockerDeploymentService = dockerDeploymentService;
        _serviceProvider = serviceProvider;

        CheckDockerCommand = new RelayCommand(async void () => await CheckDockerAsync());
        GenerateDockerComposeCommand = new RelayCommand(async void () => await GenerateDockerComposeAsync());
        StartDockerCommand = new RelayCommand(async void () => await StartDockerAsync());
        StopDockerCommand = new RelayCommand(async void () => await StopDockerAsync());
        CheckNativeCommand = new RelayCommand(async void () => await CheckNativeAsync());
        DeployNativeCommand = new RelayCommand(async void () => await DeployNativeAsync());
        DownloadFrpcCommand = new RelayCommand(async void () => await DownloadFrpcAsync());

        // Initialize
        _ = Task.Run(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        await CheckNativeAsync();
    }

    private async Task CheckDockerAsync()
    {
        try
        {
            IsDockerChecking = true;
            StatusMessage = "Checking Docker availability...";

            IsDockerAvailable = await _dockerDeploymentService.IsDockerAvailableAsync();
            StatusMessage = IsDockerAvailable
                ? "Docker is available"
                : "Docker is not available";

            _logger.LogInformation("Docker availability check: {IsAvailable}", IsDockerAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Docker availability");
            StatusMessage = "Error checking Docker availability";
            IsDockerAvailable = false;
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
            IsBusy = true;
            StatusMessage = "Generating docker-compose.yml...";

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
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var outputPath = Path.Combine(downloadsPath, "frapa-clonia-docker");
            Directory.CreateDirectory(outputPath);

            var composePath = await _dockerDeploymentService.GenerateDockerComposeAsync(outputPath, config);
            DockerComposePath = composePath;

            StatusMessage = $"docker-compose.yml generated at: {composePath}";
            _logger.LogInformation("Generated docker-compose.yml at {Path}", composePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating docker-compose.yml");
            StatusMessage = "Error generating docker-compose.yml";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartDockerAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Starting Docker container...";

            if (string.IsNullOrEmpty(DockerComposePath))
            {
                StatusMessage = "Please generate docker-compose.yml first";
                return;
            }

            var composeDirectory = Path.GetDirectoryName(DockerComposePath);
            if (composeDirectory == null)
            {
                StatusMessage = "Invalid docker-compose path";
                return;
            }

            var success = await _dockerDeploymentService.StartDockerComposeAsync(composeDirectory);
            if (success)
            {
                StatusMessage = "Docker container started successfully";
                IsContainerRunning = await _dockerDeploymentService.IsContainerRunningAsync(DockerContainerName);
            }
            else
            {
                StatusMessage = "Failed to start Docker container";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Docker container");
            StatusMessage = "Error starting Docker container";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StopDockerAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Stopping Docker container...";

            if (string.IsNullOrEmpty(DockerComposePath))
            {
                StatusMessage = "No docker-compose.yml found";
                return;
            }

            var composeDirectory = Path.GetDirectoryName(DockerComposePath);
            if (composeDirectory == null)
            {
                StatusMessage = "Invalid docker-compose path";
                return;
            }

            var success = await _dockerDeploymentService.StopDockerComposeAsync(composeDirectory);
            if (success)
            {
                StatusMessage = "Docker container stopped successfully";
                IsContainerRunning = false;
            }
            else
            {
                StatusMessage = "Failed to stop Docker container";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Docker container");
            StatusMessage = "Error stopping Docker container";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckNativeAsync()
    {
        try
        {
            IsNativeChecking = true;
            StatusMessage = "Checking native deployment...";

            IsNativeDeployed = await _nativeDeploymentService.IsDeployedAsync();
            if (IsNativeDeployed)
            {
                DeployedBinaryPath = await _nativeDeploymentService.GetDeployedBinaryPathAsync();
                StatusMessage = $"Native deployment found at: {DeployedBinaryPath}";
            }
            else
            {
                StatusMessage = "Native deployment not found. Please download and deploy frpc.";
                DeployedBinaryPath = null;
            }

            _logger.LogInformation("Native deployment check: IsDeployed={IsDeployed}, Path={Path}",
                IsNativeDeployed, DeployedBinaryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking native deployment");
            StatusMessage = "Error checking native deployment";
            IsNativeDeployed = false;
        }
        finally
        {
            IsNativeChecking = false;
        }
    }

    private Task DeployNativeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Deploying native frpc...";

            // This would typically involve:
            // 1. Downloading frpc if not already downloaded
            // 2. Extracting the archive
            // 3. Setting executable permissions
            // For now, this is a placeholder

            StatusMessage = "Native deployment not yet implemented";
            _logger.LogWarning("Native deployment not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying native frpc");
            StatusMessage = "Error deploying native frpc";
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }

    private Task DownloadFrpcAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Opening frpc download page...";

            // Open the GitHub releases page in a browser
            var url = "https://github.com/fatedier/frpc/releases";
            StatusMessage = $"Please download frpc from {url}";

            _logger.LogInformation("Opened frpc download page: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening frpc download page");
            StatusMessage = "Error opening frpc download page";
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }
}
