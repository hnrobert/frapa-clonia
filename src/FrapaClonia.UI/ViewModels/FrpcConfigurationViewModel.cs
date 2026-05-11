using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for the frpc configuration dialog
/// </summary>
public partial class FrpcConfigurationViewModel : ObservableObject
{
    private readonly ILogger<FrpcConfigurationViewModel>? _logger;
    private readonly IFrpcVersionService? _frpcVersionService;
    private readonly IFrpcDownloadService? _frpcDownloadService;
    private readonly INativeDeploymentService? _nativeDeploymentService;
    private readonly IPackageManagerService? _packageManagerService;
    private readonly IProcessManager? _processManager;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;

    // Frpc Path
    [ObservableProperty] private string _frpcBinaryPath = "";
    [ObservableProperty] private bool _isPathValid;
    [ObservableProperty] private string? _detectedVersion;
    [ObservableProperty] private bool _isDetecting;

    // Version management - GitHub release versions (for web download)
    [ObservableProperty] private List<FrpcVersionInfo> _gitHubVersions = [];
    [ObservableProperty] private FrpcVersionInfo? _selectedGitHubVersion;
    [ObservableProperty] private bool _isLoadingGitHubVersions;

    // Combined version list for display (depends on install mode)
    [ObservableProperty] private List<FrpcVersionInfo> _availableVersions = [];
    [ObservableProperty] private FrpcVersionInfo? _selectedVersion;
    [ObservableProperty] private bool _isLoadingVersions;

    // Installation method
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPackageManagerMode))]
    [NotifyPropertyChangedFor(nameof(IsWebDownloadMode))]
    [NotifyPropertyChangedFor(nameof(ShowVersionSelection))]
    private string _selectedInstallMode = "package_manager"; // "package_manager" or "web_download"

    public bool IsPackageManagerMode => SelectedInstallMode == "package_manager";
    public bool IsWebDownloadMode => SelectedInstallMode == "web_download";

    // Show version selection for web download mode OR package managers that support it
    public bool ShowVersionSelection => IsWebDownloadMode ||
                                        (IsPackageManagerMode &&
                                         SelectedPackageManager?.SupportsVersionSelection == true);

    // Package Manager
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPackageManagerVersionInfo))]
    [NotifyPropertyChangedFor(nameof(ShowVersionSelection))]
    private PackageManagerInfo? _selectedPackageManager;

    // Show info that package manager only installs latest (when it doesn't support version selection)
    public bool ShowPackageManagerVersionInfo => IsPackageManagerMode &&
                                                 SelectedPackageManager is
                                                     { CanInstallFrpc: true, SupportsVersionSelection: false };

    [ObservableProperty] private List<PackageManagerInfo> _availablePackageManagers = [];
    [ObservableProperty] private bool _isCheckingPackageManagers;
    [ObservableProperty] private bool _isInstalling;

    // Download
    [ObservableProperty] private bool _isDownloading;

    // Downloaded versions management
    [ObservableProperty] private List<DownloadedFrpcVersion> _downloadedVersions = [];
    [ObservableProperty] private bool _isLoadingDownloadedVersions;

    public bool HasDownloadedVersions => !IsLoadingDownloadedVersions && DownloadedVersions.Count > 0;
    public bool HasNoDownloadedVersions => !IsLoadingDownloadedVersions && DownloadedVersions.Count == 0;

    // Dialog result
    public bool DialogResult { get; private set; }
    public event EventHandler? CloseRequested;

    // "Latest" version placeholder for package managers that don't support version selection
    private static readonly FrpcVersionInfo LatestVersionPlaceholder = new()
    {
        Version = "latest",
        TagName = "latest",
        PublishedAt = DateTimeOffset.Now,
        IsLatest = true
    };

    public IRelayCommand AutoDetectPathCommand { get; }
    public IRelayCommand BrowsePathCommand { get; }
    public IRelayCommand RefreshVersionsCommand { get; }
    public IRelayCommand RefreshPackageManagersCommand { get; }
    public IRelayCommand InstallViaPackageManagerCommand { get; }
    public IRelayCommand DownloadDirectCommand { get; }
    public IRelayCommand OpenDownloadPageCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand UseVersionCommand { get; }
    public IRelayCommand DeleteVersionCommand { get; }
    public IRelayCommand RefreshDownloadedVersionsCommand { get; }

    // Default constructor for design-time
    public FrpcConfigurationViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<FrpcConfigurationViewModel>.Instance,
        null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public FrpcConfigurationViewModel(
        ILogger<FrpcConfigurationViewModel> logger,
        IFrpcVersionService frpcVersionService,
        IFrpcDownloadService frpcDownloadService,
        INativeDeploymentService nativeDeploymentService,
        IPackageManagerService packageManagerService,
        IProcessManager processManager,
        ToastService? toastService,
        ILocalizationService localizationService)
    {
        _logger = logger;
        _frpcVersionService = frpcVersionService;
        _frpcDownloadService = frpcDownloadService;
        _nativeDeploymentService = nativeDeploymentService;
        _packageManagerService = packageManagerService;
        _processManager = processManager;
        _toastService = toastService;
        _localizationService = localizationService;

        AutoDetectPathCommand = new RelayCommand(async void () =>
        {
            try
            {
                await AutoDetectPathAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in AutoDetectPathCommand");
            }
        });
        BrowsePathCommand = new RelayCommand(async void () =>
        {
            try
            {
                await BrowsePathAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in BrowsePathCommand");
            }
        });
        RefreshVersionsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RefreshVersionsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in RefreshVersionsCommand");
            }
        });
        RefreshPackageManagersCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RefreshPackageManagersAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in RefreshPackageManagersCommand");
            }
        });
        InstallViaPackageManagerCommand = new RelayCommand(async void () =>
        {
            try
            {
                await InstallViaPackageManagerAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in InstallViaPackageManagerCommand");
            }
        });
        DownloadDirectCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DownloadDirectAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in DownloadDirectCommand");
            }
        });
        OpenDownloadPageCommand = new RelayCommand(OpenDownloadPage);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        UseVersionCommand = new RelayCommand<DownloadedFrpcVersion?>(version =>
        {
            if (version != null)
            {
                UseVersion(version);
            }
        });
        DeleteVersionCommand = new RelayCommand<DownloadedFrpcVersion?>(version =>
        {
            if (version != null)
            {
                TogglePendingDeletion(version);
            }
        });
        RefreshDownloadedVersionsCommand = new RelayCommand(async void () =>
        {
            try
            {
                await RefreshDownloadedVersionsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in RefreshDownloadedVersionsCommand");
            }
        });
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    /// <summary>
    /// Initialize with an existing path - runs detection asynchronously
    /// </summary>
    private void Initialize(string? currentPath)
    {
        FrpcBinaryPath = currentPath ?? "";

        // Set initial loading states
        IsLoadingVersions = true;
        IsLoadingGitHubVersions = true;
        IsLoadingDownloadedVersions = true;
        IsDetecting = string.IsNullOrEmpty(FrpcBinaryPath);
        IsCheckingPackageManagers = true;

        // Run all detection operations in parallel without blocking
        _ = Task.Run(async () =>
        {
            try
            {
                // Run all operations in parallel
                var versionsTask = RefreshGitHubVersionsAsync();
                var packageManagersTask = RefreshPackageManagersAsync();
                var downloadedTask = RefreshDownloadedVersionsAsync();

                // Validate or detect path
                var pathTask = string.IsNullOrEmpty(FrpcBinaryPath) ? AutoDetectPathAsync() : ValidatePathAsync();

                // Wait for all tasks
                await Task.WhenAll(versionsTask, packageManagersTask, downloadedTask, pathTask);

                // Update available versions based on initial mode
                UpdateAvailableVersionsForMode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during initialization");
            }
        });
    }

    /// <summary>
    /// Initialize with an existing path (async version for compatibility)
    /// </summary>
    public void InitializeAsync(string? currentPath)
    {
        Initialize(currentPath);
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
                if (!File.Exists(testPath)) continue;
                FrpcBinaryPath = testPath;
                _toastService?.Success(L("Toast_FrpcDetected"), L("Toast_FrpcFoundAt", testPath));
                await ValidatePathAsync();
                return;
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
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
            {
                _toastService?.Warning(L("Toast_NotAvailable"), L("Toast_FilePickerNotAvailable"));
                return;
            }

            var storageProvider = desktop.MainWindow?.StorageProvider;
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

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDownloadedVersionsChanged(List<DownloadedFrpcVersion> value)
    {
        OnPropertyChanged(nameof(HasDownloadedVersions));
        OnPropertyChanged(nameof(HasNoDownloadedVersions));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnIsLoadingDownloadedVersionsChanged(bool value)
    {
        OnPropertyChanged(nameof(HasDownloadedVersions));
        OnPropertyChanged(nameof(HasNoDownloadedVersions));
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

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedInstallModeChanged(string value)
    {
        // Update available versions when mode changes
        UpdateAvailableVersionsForMode();
        OnPropertyChanged(nameof(ShowPackageManagerVersionInfo));
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedPackageManagerChanged(PackageManagerInfo? value)
    {
        OnPropertyChanged(nameof(ShowPackageManagerVersionInfo));
        OnPropertyChanged(nameof(ShowVersionSelection));
        // Update available versions when package manager changes
        if (IsPackageManagerMode)
        {
            UpdateAvailableVersionsForMode();
        }
    }

    partial void OnSelectedVersionChanged(FrpcVersionInfo? value)
    {
        // The version selector in the UI is bound to SelectedVersion.
        // Keep SelectedGitHubVersion in sync so download actions always use the user's selection.
        if (value == null) return;
        if (IsWebDownloadMode || SelectedPackageManager?.SupportsVersionSelection == true)
        {
            SelectedGitHubVersion = value;
        }
    }

    private void UpdateAvailableVersionsForMode()
    {
        if (IsWebDownloadMode)
        {
            // Web download mode - use GitHub versions
            AvailableVersions = GitHubVersions;
            SelectedVersion = SelectedGitHubVersion ?? AvailableVersions.FirstOrDefault();
            IsLoadingVersions = IsLoadingGitHubVersions;
        }
        else if (SelectedPackageManager?.SupportsVersionSelection == true)
        {
            // Package manager mode with version selection support - use GitHub versions
            AvailableVersions = GitHubVersions;
            SelectedVersion = SelectedGitHubVersion ?? AvailableVersions.FirstOrDefault();
            IsLoadingVersions = IsLoadingGitHubVersions;
        }
        else
        {
            // Package manager mode without version selection - only "latest"
            AvailableVersions = [LatestVersionPlaceholder];
            SelectedVersion = LatestVersionPlaceholder;
            IsLoadingVersions = false;
        }
    }

    private async Task RefreshGitHubVersionsAsync(bool forceRefresh = false)
    {
        try
        {
            IsLoadingGitHubVersions = true;
            IsLoadingVersions = true;
            _logger?.LogInformation("Refreshing available frpc versions from GitHub");

            if (_frpcVersionService != null)
            {
                var versions = await _frpcVersionService.GetAvailableVersionsAsync(forceRefresh);
                GitHubVersions = versions.ToList();

                // Select latest by default
                SelectedGitHubVersion = GitHubVersions.FirstOrDefault();

                _logger?.LogInformation("Found {Count} frpc versions from GitHub", GitHubVersions.Count);

                // Only show toast when fetched from GitHub (not from cache)
                if (_frpcVersionService.WasRateLimited)
                {
                    _toastService?.Warning(
                        L("Toast_GitHubRateLimited"),
                        L("Toast_GitHubRateLimitedDesc"));
                }
                else if (!_frpcVersionService.UsedCache)
                {
                    if (GitHubVersions.Count > 0)
                    {
                        if (forceRefresh)
                        {
                            _toastService?.Success(
                                L("Toast_FrpcVersionsFetched"),
                                L("Toast_FrpcVersionsFetchedDesc", GitHubVersions.Count));
                        }
                    }
                    else
                    {
                        _toastService?.Error(
                            L("Toast_FrpcVersionsFetchFailed"),
                            L("Toast_FrpcVersionsFetchFailedDesc"));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing GitHub versions");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotFetchVersions"));
        }
        finally
        {
            IsLoadingGitHubVersions = false;
            UpdateAvailableVersionsForMode();
        }
    }

    private async Task RefreshVersionsAsync()
    {
        if (IsWebDownloadMode)
        {
            SelectedGitHubVersion = null;
            SelectedVersion = null;
            await RefreshGitHubVersionsAsync(forceRefresh: true);
        }
        // For package manager mode, no need to refresh - always "latest"
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
                                             .FirstOrDefault(m => m is { IsInstalled: true, CanInstallFrpc: true }) ??
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

            // Determine version to install
            string? versionToInstall = null;
            if (SelectedPackageManager.SupportsVersionSelection && SelectedVersion != null &&
                SelectedVersion.Version != "latest")
            {
                versionToInstall = SelectedVersion.Version;
            }

            var versionText = versionToInstall ?? "latest";
            _toastService?.Info(L("Toast_Installing"),
                L("Toast_InstallingFrpcVia", $"{SelectedPackageManager.DisplayName} ({versionText})"));

            if (_packageManagerService != null)
            {
                var success =
                    await _packageManagerService.InstallFrpcAsync(SelectedPackageManager.Name, versionToInstall);
                if (success)
                {
                    var path = await _packageManagerService.GetFrpcBinaryPathAsync(SelectedPackageManager.Name);
                    if (!string.IsNullOrEmpty(path))
                    {
                        FrpcBinaryPath = path;
                        _toastService?.Success(L("Toast_Installed"),
                            L("Toast_FrpcInstalledVia", SelectedPackageManager.DisplayName));
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
        // UI version selector is bound to SelectedVersion.
        // Prefer it so the chosen version is always respected.
        var versionToDownload = SelectedVersion ?? SelectedGitHubVersion;
        if (versionToDownload == null)
        {
            _toastService?.Warning(L("Toast_NoVersion"), L("Toast_SelectVersionFirst"));
            return;
        }

        try
        {
            IsDownloading = true;
            _toastService?.Info(L("Toast_Downloading"), L("Toast_DownloadingFrpc", versionToDownload.Version));

            if (_frpcDownloadService != null && _nativeDeploymentService != null)
            {
                // Get the download URL - either from the version info or construct it
                var downloadUrl = versionToDownload.DownloadUrl;
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    // Construct the URL
                    downloadUrl = _frpcVersionService?.GetDownloadUrl(versionToDownload);
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    _toastService?.Error(L("Toast_DownloadFailed"), L("Toast_CouldNotGetDownloadUrl"));
                    return;
                }

                // Download to temp directory first
                var tempDir = Path.GetTempPath();
                var archivePath = await _frpcDownloadService.DownloadFromMirrorAsync(downloadUrl, tempDir);

                _toastService?.Info(L("Toast_Deploying"), L("Toast_DeployingFrpcBinary"));

                // Get platform and architecture
                var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" : "linux";
                var architecture = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "amd64",
                    Architecture.Arm64 => "arm64",
                    Architecture.X86 => "386",
                    Architecture.Arm => "arm",
                    _ => "amd64"
                };

                // Deploy with versioned folder
                var binaryPath = await _nativeDeploymentService.DeployFromArchiveAsync(
                    archivePath,
                    versionToDownload.Version,
                    platform,
                    architecture);

                FrpcBinaryPath = binaryPath;
                _toastService?.Success(L("Toast_Downloaded"), L("Toast_FrpcDownloaded", binaryPath));

                // Refresh the detected/downloaded versions list so the table updates immediately.
                await RefreshDownloadedVersionsAsync();
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

        // Apply pending deletions and close
        _ = Task.Run(async () => { await ApplyPendingDeletionsAsync(); });

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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims",
                    "frpc.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft",
                    "WinGet", "Links", "frpc.exe")
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

    #region Downloaded Versions Management

    private async Task RefreshDownloadedVersionsAsync()
    {
        if (_nativeDeploymentService == null) return;

        try
        {
            IsLoadingDownloadedVersions = true;
            _logger?.LogInformation("Refreshing all detected frpc installations");

            // Get all detected frpc installations (app downloads, package managers, PATH)
            var versions = await _nativeDeploymentService.GetAllDetectedFrpcAsync(
                _packageManagerService,
                _processManager);

            // Mark versions in use (matching current path)
            foreach (var version in versions)
            {
                version.IsInUse = !string.IsNullOrEmpty(FrpcBinaryPath) &&
                                  string.Equals(version.BinaryPath, FrpcBinaryPath, StringComparison.OrdinalIgnoreCase);
            }

            DownloadedVersions = versions.ToList();
            _logger?.LogInformation("Found {Count} frpc installations", DownloadedVersions.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing detected frpc installations");
            _toastService?.Error(L("Toast_Error"), L("Toast_CouldNotLoadVersions"));
        }
        finally
        {
            IsLoadingDownloadedVersions = false;
        }
    }

    private void UseVersion(DownloadedFrpcVersion version)
    {
        if (string.IsNullOrEmpty(version.BinaryPath) || !File.Exists(version.BinaryPath))
        {
            _toastService?.Warning(L("Toast_NotAvailable"), L("Toast_BinaryNotFound"));
            return;
        }

        // Set the path to this version's binary
        FrpcBinaryPath = version.BinaryPath;
        _toastService?.Success(L("Toast_VersionSelected"), L("Toast_WillUseVersion", version.Version));

        // Validate the new path
        _ = ValidatePathAsync();

        // Refresh to update "in use" status
        _ = RefreshDownloadedVersionsAsync();
    }

    private void TogglePendingDeletion(DownloadedFrpcVersion version)
    {
        // Cannot delete versions not managed by this app or package manager
        if (!version.CanDelete)
        {
            _toastService?.Warning(L("Toast_CannotDelete"), L("Toast_NotManagedByApp"));
            return;
        }

        if (version.IsInUse)
        {
            _toastService?.Warning(L("Toast_VersionInUse"), L("Toast_CannotDeleteUsedVersion"));
            return;
        }

        version.IsPendingDeletion = !version.IsPendingDeletion;
    }

    private async Task ApplyPendingDeletionsAsync()
    {
        var versionsToDelete = DownloadedVersions.Where(v => v.IsPendingDeletion).ToList();
        if (versionsToDelete.Count == 0) return;

        var deletedCount = 0;
        foreach (var version in versionsToDelete)
        {
            try
            {
                bool success;

                switch (version.Source)
                {
                    case FrpcSource.PackageManager when !string.IsNullOrEmpty(version.PackageManagerName):
                    {
                        // Uninstall via package manager
                        if (_packageManagerService != null)
                        {
                            success = await _packageManagerService.UninstallFrpcAsync(version.PackageManagerName);
                            if (success)
                            {
                                _logger?.LogInformation("Uninstalled frpc via package manager {PackageManager}",
                                    version.PackageManagerName);
                            }
                        }
                        else
                        {
                            success = false;
                        }

                        break;
                    }
                    case FrpcSource.AppDownload when _nativeDeploymentService != null:
                        // Delete app-downloaded version
                        success = await _nativeDeploymentService.DeleteVersionAsync(version.FolderPath);
                        break;
                    case FrpcSource.SystemPath:
                    case FrpcSource.Manual:
                    default:
                        // Cannot delete other sources
                        _logger?.LogWarning("Cannot delete version {Version} from source {Source}",
                            version.Version, version.Source);
                        success = false;
                        break;
                }

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