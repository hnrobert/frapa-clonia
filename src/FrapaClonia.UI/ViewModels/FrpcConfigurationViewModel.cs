using System.Runtime.InteropServices;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for the frpc configuration dialog
/// </summary>
public partial class FrpcConfigurationViewModel : ObservableObject
{
    private readonly ILogger<FrpcConfigurationViewModel>? _logger;
    private readonly IFrpcVersionService? _frpcVersionService;
    private readonly IFrpcDownloader? _frpcDownloader;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IPackageManagerService? _packageManagerService;
    private readonly IProcessManager? _processManager;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;

    // Frpc Path
    [ObservableProperty] private string _frpcBinaryPath = "";
    [ObservableProperty] private bool _isPathValid;
    [ObservableProperty] private string? _detectedVersion;
    [ObservableProperty] private bool _isDetecting;

    // Version management
    [ObservableProperty] private List<FrpcVersionInfo> _availableVersions = [];
    [ObservableProperty] private FrpcVersionInfo? _selectedVersion;
    [ObservableProperty] private bool _isLoadingVersions;

    // Installation method
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPackageManagerMode))]
    [NotifyPropertyChangedFor(nameof(IsWebDownloadMode))]
    private string _selectedInstallMode = "package_manager"; // "package_manager" or "web_download"

    public bool IsPackageManagerMode => SelectedInstallMode == "package_manager";
    public bool IsWebDownloadMode => SelectedInstallMode == "web_download";

    // Package Manager
    [ObservableProperty] private List<PackageManagerInfo> _availablePackageManagers = [];
    [ObservableProperty] private PackageManagerInfo? _selectedPackageManager;
    [ObservableProperty] private bool _isCheckingPackageManagers;
    [ObservableProperty] private bool _isInstalling;

    // Download
    [ObservableProperty] private bool _isDownloading;

    // Dialog result
    public bool DialogResult { get; private set; }
    public event EventHandler? CloseRequested;

    public IRelayCommand AutoDetectPathCommand { get; }
    public IRelayCommand BrowsePathCommand { get; }
    public IRelayCommand RefreshVersionsCommand { get; }
    public IRelayCommand RefreshPackageManagersCommand { get; }
    public IRelayCommand InstallViaPackageManagerCommand { get; }
    public IRelayCommand DownloadDirectCommand { get; }
    public IRelayCommand OpenDownloadPageCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    // Default constructor for design-time
    public FrpcConfigurationViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<FrpcConfigurationViewModel>.Instance,
        null!, null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public FrpcConfigurationViewModel(
        ILogger<FrpcConfigurationViewModel> logger,
        IFrpcVersionService frpcVersionService,
        IFrpcDownloader frpcDownloader,
        INativeDeploymentService nativeDeploymentService,
        IPackageManagerService packageManagerService,
        IProcessManager processManager,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        ILocalizationService? localizationService)
    {
        _logger = logger;
        _frpcVersionService = frpcVersionService;
        _frpcDownloader = frpcDownloader;
        _nativeDeploymentService = nativeDeploymentService;
        _packageManagerService = packageManagerService;
        _processManager = processManager;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _localizationService = localizationService;

        AutoDetectPathCommand = new RelayCommand(async () => await AutoDetectPathAsync());
        BrowsePathCommand = new RelayCommand(async () => await BrowsePathAsync());
        RefreshVersionsCommand = new RelayCommand(async () => await RefreshVersionsAsync());
        RefreshPackageManagersCommand = new RelayCommand(async () => await RefreshPackageManagersAsync());
        InstallViaPackageManagerCommand = new RelayCommand(async () => await InstallViaPackageManagerAsync());
        DownloadDirectCommand = new RelayCommand(async () => await DownloadDirectAsync());
        OpenDownloadPageCommand = new RelayCommand(OpenDownloadPage);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    /// <summary>
    /// Initialize with an existing path
    /// </summary>
    public async Task InitializeAsync(string? currentPath)
    {
        FrpcBinaryPath = currentPath ?? "";

        // Load versions
        await RefreshVersionsAsync();

        // Auto-detect if no path set
        if (string.IsNullOrEmpty(FrpcBinaryPath))
        {
            await AutoDetectPathAsync();
        }
        else
        {
            await ValidatePathAsync();
        }

        // Load package managers
        await RefreshPackageManagersAsync();
    }

    private async Task AutoDetectPathAsync()
    {
        try
        {
            IsDetecting = true;
            _logger?.LogInformation("Auto-detecting frpc in PATH");

            var whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var result = await _processManager!.ExecuteAsync(whichCmd, "frpc");

            if (result.Success)
            {
                var path = result.StandardOutput.Split('\n').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    FrpcBinaryPath = path;
                    _toastService?.Success(L("Toast_FrpcDetected"), L("Toast_FrpcFoundAt", path));
                    await ValidatePathAsync();
                    return;
                }
            }

            // Also check common locations
            var commonPaths = GetCommonBinaryPaths();
            foreach (var testPath in commonPaths)
            {
                if (File.Exists(testPath))
                {
                    FrpcBinaryPath = testPath;
                    _toastService?.Success(L("Toast_FrpcDetected"), L("Toast_FrpcFoundAt", testPath));
                    await ValidatePathAsync();
                    return;
                }
            }

            IsPathValid = false;
            DetectedVersion = null;
            _toastService?.Info(L("Toast_FrpcNotFound"), L("Toast_FrpcNotInPath"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error auto-detecting frpc path");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotDetectFrpc"));
        }
        finally
        {
            IsDetecting = false;
        }
    }

    private async Task ValidatePathAsync()
    {
        if (string.IsNullOrEmpty(FrpcBinaryPath))
        {
            IsPathValid = false;
            DetectedVersion = null;
            return;
        }

        // If the path is just "frpc" (from PATH), try to find the actual path
        if (FrpcBinaryPath == "frpc" || (!Path.IsPathRooted(FrpcBinaryPath) && !File.Exists(FrpcBinaryPath)))
        {
            var whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var result = await _processManager!.ExecuteAsync(whichCmd, "frpc");
            if (result.Success)
            {
                var fullPath = result.StandardOutput.Split('\n').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    FrpcBinaryPath = fullPath;
                }
            }
        }

        if (!File.Exists(FrpcBinaryPath))
        {
            IsPathValid = false;
            DetectedVersion = null;
            return;
        }

        try
        {
            IsDetecting = true;
            var version = await _frpcVersionService!.GetBinaryVersionAsync(FrpcBinaryPath);
            if (version != null)
            {
                IsPathValid = true;
                DetectedVersion = version.Version;
                _logger?.LogInformation("Frpc version detected: {Version}", DetectedVersion);
            }
            else
            {
                IsPathValid = false;
                DetectedVersion = null;
                _toastService?.Warning(L("Toast_InvalidBinary"), L("Toast_CouldNotGetVersion"));
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
            IsDetecting = false;
        }
    }

    private async Task BrowsePathAsync()
    {
        try
        {
            if (_serviceProvider == null) return;

            var storageProvider = _serviceProvider.GetService<IStorageProvider>();
            if (storageProvider == null)
            {
                _toastService?.Warning(L("Toast_NotAvailable"), L("Toast_FilePickerNotAvailable"));
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = L("SelectFrpcBinary"),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Executable")
                    {
                        Patterns = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? ["*.exe"]
                            : ["*"]
                    }
                ]
            });

            var file = files.FirstOrDefault();
            if (file != null)
            {
                FrpcBinaryPath = file.Path.LocalPath;
                await ValidatePathAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error browsing for frpc path");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotSelectFile"));
        }
    }

    partial void OnFrpcBinaryPathChanged(string value)
    {
        // Validate when path changes (with debounce would be better, but keeping simple)
        if (!string.IsNullOrEmpty(value) && !IsDetecting)
        {
            _ = ValidatePathAsync();
        }
        else if (string.IsNullOrEmpty(value))
        {
            IsPathValid = false;
            DetectedVersion = null;
        }
    }

    private async Task RefreshVersionsAsync()
    {
        try
        {
            IsLoadingVersions = true;
            _logger?.LogInformation("Refreshing available frpc versions");

            if (_frpcVersionService != null)
            {
                var versions = await _frpcVersionService.GetAvailableVersionsAsync();
                AvailableVersions = versions.ToList();

                // Select latest by default
                SelectedVersion = AvailableVersions.FirstOrDefault();

                _logger?.LogInformation("Found {Count} frpc versions", AvailableVersions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing versions");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotFetchVersions"));
        }
        finally
        {
            IsLoadingVersions = false;
        }
    }

    private async Task RefreshPackageManagersAsync()
    {
        try
        {
            IsCheckingPackageManagers = true;
            _logger?.LogInformation("Detecting available package managers");

            if (_packageManagerService != null)
            {
                var managers = await _packageManagerService.DetectAvailablePackageManagersAsync();
                AvailablePackageManagers = managers.ToList();

                // Select first installed manager that can install frpc
                SelectedPackageManager = AvailablePackageManagers
                    .FirstOrDefault(m => m.IsInstalled && m.CanInstallFrpc) ??
                    AvailablePackageManagers.FirstOrDefault(m => m.IsInstalled);

                _logger?.LogInformation("Found {Count} package managers", AvailablePackageManagers.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error detecting package managers");
        }
        finally
        {
            IsCheckingPackageManagers = false;
        }
    }

    private async Task InstallViaPackageManagerAsync()
    {
        if (SelectedPackageManager == null || !SelectedPackageManager.CanInstallFrpc)
        {
            _toastService?.Warning(L("Toast_NotAvailable"), L("Toast_PackageManagerNotAvailable"));
            return;
        }

        try
        {
            IsInstalling = true;
            _toastService?.Info(L("Toast_Installing"), L("Toast_InstallingFrpcVia", SelectedPackageManager.DisplayName));

            if (_packageManagerService != null)
            {
                var success = await _packageManagerService.InstallFrpcAsync(SelectedPackageManager.Name);
                if (success)
                {
                    var path = await _packageManagerService.GetFrpcBinaryPathAsync(SelectedPackageManager.Name);
                    if (!string.IsNullOrEmpty(path))
                    {
                        FrpcBinaryPath = path;
                        _toastService?.Success(L("Toast_Installed"), L("Toast_FrpcInstalledVia", SelectedPackageManager.DisplayName));
                    }
                }
                else
                {
                    _toastService?.Error(L("Toast_InstallFailed"), L("Toast_CouldNotInstallFrpc"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error installing via package manager");
            _toastService?.Error(L("Toast_Error"), L("Toast_InstallFailedWithError", ex.Message));
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private async Task DownloadDirectAsync()
    {
        if (SelectedVersion == null)
        {
            _toastService?.Warning(L("Toast_NoVersion"), L("Toast_SelectVersionFirst"));
            return;
        }

        try
        {
            IsDownloading = true;
            _toastService?.Info(L("Toast_Downloading"), L("Toast_DownloadingFrpc", SelectedVersion.Version));

            if (_frpcDownloader != null && _nativeDeploymentService != null)
            {
                // Create a FrpRelease for the downloader
                var release = new FrpRelease
                {
                    TagName = SelectedVersion.TagName,
                    Version = SelectedVersion.Version,
                    PublishedAt = SelectedVersion.PublishedAt,
                    HtmlUrl = "",
                    Assets = string.IsNullOrEmpty(SelectedVersion.DownloadUrl)
                        ? []
                        : [new FrpAsset
                        {
                            Name = $"frp_{SelectedVersion.Version}",
                            DownloadUrl = SelectedVersion.DownloadUrl,
                            Size = 0,
                            Platform = "",
                            Architecture = []
                        }]
                };

                var targetDirectory = _nativeDeploymentService.GetDefaultDeploymentDirectory();
                var archivePath = await _frpcDownloader.DownloadVersionAsync(release, targetDirectory);

                _toastService?.Info(L("Toast_Deploying"), L("Toast_DeployingFrpcBinary"));

                var binaryPath = await _nativeDeploymentService.DeployFromArchiveAsync(archivePath, targetDirectory);

                // Set executable permissions on Unix
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await _nativeDeploymentService.SetExecutablePermissionsAsync(binaryPath);
                }

                FrpcBinaryPath = binaryPath;
                _toastService?.Success(L("Toast_Downloaded"), L("Toast_FrpcDownloaded", binaryPath));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading frpc directly");
            _toastService?.Error(L("Toast_DownloadFailed"), L("Toast_CouldNotDownloadFrpc", ex.Message));
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private void OpenDownloadPage()
    {
        try
        {
            var url = "https://github.com/fatedier/frp/releases";
            _toastService?.Info(L("Toast_Download"), L("Toast_OpeningUrl", url));

            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening download page");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotOpenUrl"));
        }
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(FrpcBinaryPath))
        {
            _toastService?.Warning(L("Toast_NoPath"), L("Toast_SelectFrpcPath"));
            return;
        }

        DialogResult = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        DialogResult = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

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
