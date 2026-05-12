using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.UI.Utils;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ConfigPreset = FrapaClonia.Shared.Models.ConfigPreset;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for deployment management
/// </summary>
public partial class DeploymentViewModel : ObservableObject
{
    private sealed record DockerComposeSnapshot(
        string ImageName,
        string ImageTag,
        string ContainerName,
        string RestartPolicy);

    private DockerComposeSnapshot? _composeSnapshot;
    private bool _suppressComposeDirtyTracking;
    private bool _suppressDockerAutoRefresh;
    private bool _suppressDockerImageReset;
    private bool _suppressDockerImageTagCoercion;
    private bool _suppressDockerImageTagIndexSync;
    private bool _pendingDockerImageTagResync;

    private string _lastKnownDockerImageTag = "latest";

    private bool
        _suppressComposeAutoLoad; // prevents OnDockerComposePathChanged from double-loading during async LoadFromPresetAsync

    // Track the last value that was actually sent for remote validation so LostFocus is a no-op when unchanged.
    private string _lastValidatedContainerName = "";
    private string _lastValidatedContainerComposePath = "";
    private string _lastValidatedImageName = "";

    private readonly ILogger<DeploymentViewModel>? _logger;
    private readonly IFrpcVersionService? _frpcVersionService;
    private readonly IDockerDeploymentService? _dockerDeploymentService;
    private readonly ISystemServiceManager? _systemServiceManager;
    private readonly IProcessManager? _processManager;
    private readonly IServiceProvider? _serviceProvider;
    private string? _activeServiceName;
    private readonly ToastService? _toastService;
    private readonly ILocalizationService? _localizationService;
    private readonly IPresetService? _presetService;
    private readonly NavigationService? _navigationService;

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

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LocalizedServiceState))]
    private ServiceStatus? _serviceStatus;

    /// <summary>
    /// Gets the localized service state string
    /// </summary>
    public string LocalizedServiceState => ServiceStatus?.State switch
    {
        "running" => L("StatusRunning"),
        "stopped" => L("StatusStopped"),
        "not_running" => L("StatusNotRunning"),
        "not_installed" => L("StatusNotInstalled"),
        _ => L("StatusUnknown")
    };

    private ServiceScope GetServiceScopeEnum() =>
        ServiceScopeValue == "system" ? ServiceScope.System : ServiceScope.User;

    #endregion

    #region Docker Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameConflict))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameChecking))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageConflict))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageChecking))]
    private bool _isDockerAvailable;

    [ObservableProperty] private bool _isDockerChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameConflict))]
    private string _dockerContainerName = "frapa-clonia-frpc";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameConflict))]
    private bool _hasDockerContainerNameChecked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameConflict))]
    private bool _isDockerContainerNameAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameConflict))]
    [NotifyPropertyChangedFor(nameof(ShowDockerContainerNameChecking))]
    private bool _isDockerContainerNameChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageConflict))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageChecking))]
    private string _dockerImageName = "fatedier/frpc";

    [ObservableProperty] private string _dockerImageTag = "latest";
    [ObservableProperty] private List<string> _dockerImageTags = ["latest"];
    [ObservableProperty] private int _dockerImageTagSelectedIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageConflict))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageChecking))]
    private bool _isDockerImageTagsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageConflict))]
    private bool _hasDockerImageChecked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageOk))]
    [NotifyPropertyChangedFor(nameof(ShowDockerImageConflict))]
    private bool _isDockerImageAvailable;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DockerRestartPolicyHelp))]
    private string _dockerRestartPolicy = "unless-stopped";

    public IReadOnlyList<string> DockerRestartPolicies { get; } =
        ["no", "always", "on-failure", "unless-stopped"];

    public string DockerRestartPolicyHelp => DockerRestartPolicy switch
    {
        "no" => L("RestartPolicyHelp_No"),
        "always" => L("RestartPolicyHelp_Always"),
        "on-failure" => L("RestartPolicyHelp_OnFailure"),
        "unless-stopped" => L("RestartPolicyHelp_UnlessStopped"),
        _ => ""
    };

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanRestoreDockerComposeFromFile))]
    private string _dockerComposePath = "";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanRestoreDockerComposeFromFile))]
    private bool _isDockerComposeDirty;

    public bool CanRestoreDockerComposeFromFile =>
        !string.IsNullOrWhiteSpace(DockerComposePath) &&
        File.Exists(DockerComposePath) &&
        IsDockerComposeDirty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocalizedContainerState))]
    [NotifyPropertyChangedFor(nameof(DockerPrimaryActionText))]
    [NotifyPropertyChangedFor(nameof(CanStopDockerContainer))]
    private bool _isContainerRunning;

    private CancellationTokenSource? _dockerTagsCts;

    private CancellationTokenSource? _dockerContainerNameCts;

    public bool ShowDockerContainerNameOk =>
        IsDockerAvailable && HasDockerContainerNameChecked && IsDockerContainerNameAvailable &&
        !IsDockerContainerNameChecking;

    public bool ShowDockerContainerNameConflict =>
        IsDockerAvailable && HasDockerContainerNameChecked && !IsDockerContainerNameAvailable &&
        !IsDockerContainerNameChecking;

    public bool ShowDockerContainerNameChecking => IsDockerAvailable && IsDockerContainerNameChecking;

    public bool ShowDockerImageOk =>
        IsDockerAvailable && !string.IsNullOrWhiteSpace(DockerImageName) && HasDockerImageChecked &&
        IsDockerImageAvailable &&
        !IsDockerImageTagsLoading;

    public bool ShowDockerImageConflict =>
        IsDockerAvailable && !string.IsNullOrWhiteSpace(DockerImageName) && HasDockerImageChecked &&
        !IsDockerImageAvailable &&
        !IsDockerImageTagsLoading;

    public bool ShowDockerImageChecking =>
        IsDockerAvailable && !string.IsNullOrWhiteSpace(DockerImageName) && IsDockerImageTagsLoading;

    public string LocalizedContainerState => IsContainerRunning ? L("StatusRunning") : L("StatusStopped");

    public string DockerPrimaryActionText => IsContainerRunning ? L("RecreateContainer") : L("StartContainer");

    public bool CanStopDockerContainer => IsContainerRunning;

    #endregion

    public IRelayCommand CheckFrpcPathCommand { get; }
    public IRelayCommand ConfigureFrpcCommand { get; }
    public IRelayCommand RefreshServiceStatusCommand { get; }
    public IRelayCommand InstallServiceCommand { get; }
    public IRelayCommand UninstallServiceCommand { get; }
    public IRelayCommand StartServiceCommand { get; }
    public IRelayCommand StopServiceCommand { get; }
    public IRelayCommand ViewLogsCommand { get; }
    public IRelayCommand CheckDockerCommand { get; }
    public IRelayCommand GenerateDockerComposeCommand { get; }
    public IRelayCommand SaveDockerComposeCommand { get; }
    public IRelayCommand StartDockerCommand { get; }
    public IRelayCommand StopDockerCommand { get; }
    public IRelayCommand RefreshContainerStatusCommand { get; }
    public IRelayCommand RefreshDockerImageTagsCommand { get; }
    public IRelayCommand ValidateDockerContainerNameCommand { get; }
    public IRelayCommand ValidateDockerImageCommand { get; }
    public IRelayCommand RestoreDockerComposeFromFileCommand { get; }

    // Default constructor for design-time support
    public DeploymentViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DeploymentViewModel>.Instance,
        null!, null!, null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public DeploymentViewModel(
        ILogger<DeploymentViewModel> logger,
        IFrpcVersionService frpcVersionService,
        IDockerDeploymentService dockerDeploymentService,
        ISystemServiceManager systemServiceManager,
        IProcessManager processManager,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        ILocalizationService? localizationService,
        IPresetService? presetService,
        NavigationService? navigationService)
    {
        _logger = logger;
        _frpcVersionService = frpcVersionService;
        _dockerDeploymentService = dockerDeploymentService;
        _systemServiceManager = systemServiceManager;
        _processManager = processManager;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _localizationService = localizationService;
        _presetService = presetService;
        _navigationService = navigationService;

        CheckFrpcPathCommand = CreateAsyncCommand(CheckFrpcPathAsync, "Error checking frpc path");
        ConfigureFrpcCommand = CreateAsyncCommand(ConfigureFrpcAsync, "Error opening configuration");
        RefreshServiceStatusCommand = CreateAsyncCommand(RefreshServiceStatusAsync, "Error refreshing service status");
        InstallServiceCommand = CreateAsyncCommand(InstallServiceAsync, "Error installing service");
        UninstallServiceCommand = CreateAsyncCommand(UninstallServiceAsync, "Error uninstalling service");
        StartServiceCommand = CreateAsyncCommand(StartServiceAsync, "Error starting service");
        StopServiceCommand = CreateAsyncCommand(StopServiceAsync, "Error stopping service");
        ViewLogsCommand = new RelayCommand(NavigateToLogs);
        CheckDockerCommand =
            CreateAsyncCommand(() => CheckDockerAsync(showToast: true), "Error checking Docker availability");
        GenerateDockerComposeCommand =
            CreateAsyncCommand(GenerateDockerComposeAsync, "Error generating docker compose");
        SaveDockerComposeCommand =
            CreateAsyncCommand(SaveDockerComposeAsync, "Error saving docker compose");
        StartDockerCommand = CreateAsyncCommand(StartDockerAsync, "Error starting docker");
        StopDockerCommand = CreateAsyncCommand(StopDockerAsync, "Error stopping docker");
        RefreshContainerStatusCommand =
            CreateAsyncCommand(RefreshContainerStatusAsync, "Error refreshing docker container status");
        RefreshDockerImageTagsCommand =
            CreateAsyncCommand(() => RefreshDockerImageTagsAsync(showToast: true), "Error refreshing docker tags");
        ValidateDockerContainerNameCommand =
            CreateAsyncCommand(() => ValidateDockerContainerNameAsync(showToast: false),
                "Error validating docker container name");
        ValidateDockerImageCommand =
            CreateAsyncCommand(() => RefreshDockerImageTagsAsync(showToast: false),
                "Error validating docker image");
        RestoreDockerComposeFromFileCommand =
            CreateAsyncCommand(RestoreDockerComposeFromFileAsync, "Error restoring docker compose from file");

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
        }
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        // Reload deployment settings when preset changes
        if (_presetService?.CurrentPreset == null) return;
        _ = LoadFromPresetAndInitAsync(_presetService.CurrentPreset);
    }

    private async Task LoadFromPresetAndInitAsync(ConfigPreset preset)
    {
        await LoadFromPresetAsync(preset);

        // Auto-detect if no saved path exists or the saved path is invalid
        if (string.IsNullOrEmpty(FrpcBinaryPath) || !File.Exists(FrpcBinaryPath))
        {
            _ = AutoDetectFrpcPathAsync();
        }
        else
        {
            _ = ValidateFrpcPathAsync(FrpcBinaryPath);
        }

        _ = RefreshServiceStatusAsync();
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
        // Load saved settings from current preset first
        if (_presetService?.CurrentPreset != null)
        {
            await LoadFromPresetAsync(_presetService.CurrentPreset);
            _logger?.LogInformation("Loaded deployment settings from preset: {FrpcBinaryPath}", FrpcBinaryPath);
        }

        // Only auto-detect if no saved path exists or the saved path is invalid
        if (string.IsNullOrEmpty(FrpcBinaryPath) || !File.Exists(FrpcBinaryPath))
        {
            await AutoDetectFrpcPathAsync();
        }
        else
        {
            // Validate the saved path
            await ValidateFrpcPathAsync(FrpcBinaryPath);
        }

        // Refresh service status
        await RefreshServiceStatusAsync();

        // Auto-check Docker availability if Docker mode is selected
        if (IsDockerMode)
        {
            await CheckDockerAsync(showToast: false);
        }
    }

    private string L(string key, params object[] args) =>
        _localizationService?.GetString(key, args) ?? key;

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedDeploymentModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsNativeMode));
        OnPropertyChanged(nameof(IsDockerMode));

        // Persist selection immediately so Native/Docker mode is remembered per preset.
        if (_presetService?.CurrentPreset != null)
        {
            var currentPresetMode = _presetService.CurrentPreset.Deployment.DeploymentMode;
            if (!string.Equals(currentPresetMode, value, StringComparison.Ordinal))
            {
                _ = PersistCurrentPresetAsync();
            }
        }

        if (_suppressDockerAutoRefresh) return;

        // Auto-check Docker availability when switching to Docker mode
        if (value != "docker" || IsDockerChecking) return;
        _ = CheckDockerAsync(showToast: false);
        _ = RefreshDockerImageTagsAsync(showToast: false);
    }

    private async Task RestoreDockerComposeFromFileAsync()
    {
        if (string.IsNullOrWhiteSpace(DockerComposePath)) return;
        if (!File.Exists(DockerComposePath)) return;
        await LoadDockerComposeFromFileAsync(DockerComposePath);
    }

    private async Task PersistCurrentPresetAsync()
    {
        try
        {
            if (_presetService?.CurrentPreset == null)
            {
                return;
            }

            SaveToPreset(_presetService.CurrentPreset);
            await _presetService.SaveCurrentPresetAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to persist current preset");
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerImageNameChanged(string value)
    {
        UpdateDockerComposeDirtyFlag();

        if (_suppressDockerImageReset)
        {
            return;
        }

        HasDockerImageChecked = false;
        IsDockerImageAvailable = false;
        DockerImageTags = [];

        if (IsDockerMode && !_suppressDockerAutoRefresh)
        {
            _ = RefreshDockerImageTagsAsync(showToast: false);
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerContainerNameChanged(string value)
    {
        UpdateDockerComposeDirtyFlag();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerImageTagChanged(string value)
    {
        // Avalonia ComboBox may transiently clear SelectedItem when ItemsSource is replaced.
        // If we accept that empty value, the binding can get stuck showing blank until the user
        // forces another reload (e.g. by clicking Restore). Instead, coerce empties back to the
        // last known non-empty tag (prefer the one loaded from docker-compose.yml).
        if (string.IsNullOrWhiteSpace(value))
        {
            if (_suppressDockerImageTagCoercion)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_lastKnownDockerImageTag)) return;
            _suppressDockerImageTagCoercion = true;

            try
            {
                DockerImageTag = _lastKnownDockerImageTag;
            }
            finally
            {
                _suppressDockerImageTagCoercion = false;
            }

            return;
        }

        _lastKnownDockerImageTag = value.Trim();
        SyncDockerImageTagSelectedIndex();
        UpdateDockerComposeDirtyFlag();
    }

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerImageTagsChanged(List<string> value)
    {
        SyncDockerImageTagSelectedIndex();
    }

    partial void OnDockerImageTagSelectedIndexChanged(int value)
    {
        if (_suppressDockerImageTagIndexSync)
        {
            return;
        }

        // When ItemsSource changes, Avalonia may temporarily clear selection and push -1 into the binding.
        // If we accept -1, the ComboBox can stay unselected even though DockerImageTag is valid.
        // Resync on the UI thread after the control settles.
        if (value < 0)
        {
            if (_pendingDockerImageTagResync)
            {
                return;
            }

            if (DockerImageTags.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(DockerImageTag) && string.IsNullOrWhiteSpace(_lastKnownDockerImageTag))
            {
                return;
            }

            _pendingDockerImageTagResync = true;
            Dispatcher.UIThread.Post(() =>
            {
                _pendingDockerImageTagResync = false;
                SyncDockerImageTagSelectedIndex();
            }, DispatcherPriority.Background);

            return;
        }

        if (value >= DockerImageTags.Count)
        {
            return;
        }

        var selected = DockerImageTags[value];
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        if (string.Equals(DockerImageTag, selected, StringComparison.Ordinal))
        {
            return;
        }

        _suppressDockerImageTagCoercion = true;
        try
        {
            DockerImageTag = selected;
        }
        finally
        {
            _suppressDockerImageTagCoercion = false;
        }
    }

    private void SyncDockerImageTagSelectedIndex()
    {
        if (_suppressDockerImageTagIndexSync)
        {
            return;
        }

        if (DockerImageTags.Count == 0)
        {
            return;
        }

        var tag = DockerImageTag.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var index = DockerImageTags.FindIndex(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            index = 0;
        }

        // If the computed index is the same as the current value, ItemsSource refresh may have
        // cleared the ComboBox selection without changing the bound VM value. In that case,
        // force a property change notification so the binding re-applies the selection.
        if (DockerImageTagSelectedIndex == index)
        {
            OnPropertyChanged(nameof(DockerImageTagSelectedIndex));
            return;
        }

        _suppressDockerImageTagIndexSync = true;
        try
        {
            DockerImageTagSelectedIndex = index;
        }
        finally
        {
            _suppressDockerImageTagIndexSync = false;
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerRestartPolicyChanged(string value)
    {
        UpdateDockerComposeDirtyFlag();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDockerComposePathChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _composeSnapshot = null;
            IsDockerComposeDirty = false;
            HasDockerContainerNameChecked = false;
            IsDockerContainerNameAvailable = false;
            return;
        }

        if (!File.Exists(value))
        {
            _composeSnapshot = null;
            IsDockerComposeDirty = false;
            HasDockerContainerNameChecked = false;
            IsDockerContainerNameAvailable = false;
            return;
        }

        HasDockerContainerNameChecked = false;
        IsDockerContainerNameAvailable = false;

        // Suppressed when LoadFromPresetAsync handles loading directly (avoids double-load).
        if (_suppressComposeAutoLoad) return;

        _ = LoadDockerComposeFromFileAsync(value);
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
                if (!File.Exists(testPath)) continue;
                FrpcBinaryPath = testPath;
                await ValidateFrpcPathAsync(testPath);
                return;
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

        // If the configuration dialog is already open, bring it to front.
        if (WindowReuse.ActivateExisting<FrpcConfigurationDialog>() != null)
        {
            return;
        }

        try
        {
            if (_localizationService != null)
            {
                var viewModel = new FrpcConfigurationViewModel(
                    _serviceProvider.GetRequiredService<ILogger<FrpcConfigurationViewModel>>(),
                    _serviceProvider.GetRequiredService<IFrpcVersionService>(),
                    _serviceProvider.GetRequiredService<IFrpcDownloadService>(),
                    _serviceProvider.GetRequiredService<INativeDeploymentService>(),
                    _serviceProvider.GetRequiredService<IPackageManagerService>(),
                    _serviceProvider.GetRequiredService<IProcessManager>(),
                    _toastService,
                    _localizationService);

                viewModel.InitializeAsync(FrpcBinaryPath);

                var dialog = new FrpcConfigurationDialog(viewModel);

                // Get the main window
                var mainWindow = _serviceProvider.GetService<Window>();
                bool? dialogResult;

                if (mainWindow != null)
                {
                    dialogResult = await dialog.ShowDialog<bool?>(mainWindow);
                }
                else
                {
                    dialog.Show();
                    // For non-modal dialog, we need to wait for close
                    var tcs = new TaskCompletionSource<bool>();
                    dialog.Closed += (_, _) => tcs.TrySetResult(viewModel.DialogResult);
                    await tcs.Task;
                    dialogResult = viewModel.DialogResult;
                }

                if (dialogResult == true && !string.IsNullOrEmpty(viewModel.FrpcBinaryPath))
                {
                    FrpcBinaryPath = viewModel.FrpcBinaryPath;
                    await ValidateFrpcPathAsync(FrpcBinaryPath);

                    // Save to preset and persist to file
                    if (_presetService?.CurrentPreset != null)
                    {
                        SaveToPreset(_presetService.CurrentPreset);
                        await _presetService.SaveCurrentPresetAsync();
                        _logger?.LogInformation("Saved frpc binary path to preset: {Path}", FrpcBinaryPath);
                    }
                }
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
                var serviceName = _systemServiceManager.GetServiceNameForPreset(_presetService!.CurrentPreset!.Id);
                ServiceStatus = await _systemServiceManager.GetServiceStatusAsync(serviceName, GetServiceScopeEnum());

                _activeServiceName = ServiceStatus.IsInstalled ? serviceName : null;
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

            var configPath = _presetService!.GetPresetFrpcConfigPath(_presetService!.CurrentPreset!.Id);

            // Save config first
            await _presetService.SaveCurrentPresetAsync();

            var serviceName = _systemServiceManager.GetServiceNameForPreset(_presetService!.CurrentPreset!.Id);

            var config = new ServiceConfig
            {
                ServiceName = serviceName,
                BinaryPath = FrpcBinaryPath,
                ConfigPath = configPath,
                Scope = GetServiceScopeEnum(),
                AutoStart = AutoStartOnBoot
            };

            // Notify user that admin privileges may be required for system-level services
            if (config.Scope == ServiceScope.System)
            {
                _toastService?.Info(L("Toast_AdminRequired"), L("Toast_AdminRequiredMessage"));
            }

            var success = await _systemServiceManager.InstallServiceAsync(config);
            if (success)
            {
                _activeServiceName = serviceName;
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

            var serviceName = _activeServiceName ?? _systemServiceManager.GetServiceNameForPreset(_presetService!.CurrentPreset!.Id);
            var success = await _systemServiceManager.UninstallServiceAsync(serviceName);

            if (success)
            {
                _toastService?.Success(L("Toast_ServiceUninstalled"), L("Toast_FrpcServiceUninstalled"));
            }
            else
            {
                _toastService?.Error(L("Toast_UninstallFailed"), L("Toast_CouldNotUninstallService"));
            }

            // Always refresh status after operation
            await RefreshServiceStatusAsync();
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

            var serviceName = _activeServiceName ?? _systemServiceManager.GetServiceNameForPreset(_presetService!.CurrentPreset!.Id);
            var scope = GetServiceScopeEnum();
            var success = await _systemServiceManager.StartServiceAsync(serviceName, scope);

            if (success)
            {
                // Wait a moment for the service to actually start
                await Task.Delay(500);

                // Refresh status to verify service actually started
                await RefreshServiceStatusAsync();

                // Check if the service is actually running
                if (IsServiceRunning)
                {
                    _toastService?.Success(L("Toast_ServiceStarted"), L("Toast_FrpcServiceStarted"));
                }
                else
                {
                    // Service start command succeeded but service is not running
                    _toastService?.Warning(L("Toast_Warning"), L("Toast_ServiceStartFailedVerification"));
                    _logger?.LogWarning("Service start command succeeded but service is not running");
                }
            }
            else
            {
                _toastService?.Error(L("Toast_StartFailed"), L("Toast_CouldNotStartService"));
                await RefreshServiceStatusAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceStartFailed", ex.Message));
        }
    }

    private void NavigateToLogs()
    {
        _navigationService?.NavigateTo("logs");
    }

    private async Task StopServiceAsync()
    {
        try
        {
            if (_systemServiceManager == null) return;

            var serviceName = _activeServiceName ?? _systemServiceManager.GetServiceNameForPreset(_presetService!.CurrentPreset!.Id);
            var scope = GetServiceScopeEnum();
            var success = await _systemServiceManager.StopServiceAsync(serviceName, scope);

            if (success)
            {
                _toastService?.Success(L("Toast_ServiceStopped"), L("Toast_FrpcServiceStopped"));
            }
            else
            {
                _toastService?.Error(L("Toast_StopFailed"), L("Toast_CouldNotStopService"));
            }

            // Always refresh status after operation
            await RefreshServiceStatusAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping service");
            _toastService?.Error(L("Toast_Error"), L("Toast_ServiceStopFailed", ex.Message));
        }
    }

    #endregion

    #region Docker Methods

    private async Task CheckDockerAsync(bool showToast = true)
    {
        try
        {
            IsDockerChecking = true;

            if (_dockerDeploymentService != null)
            {
                IsDockerAvailable = await _dockerDeploymentService.IsDockerAvailableAsync();
            }

            if (showToast)
            {
                if (IsDockerAvailable)
                {
                    _toastService?.Success(L("Toast_DockerAvailable"), L("Toast_DockerReady"));
                }
                else
                {
                    _toastService?.Warning(L("Toast_DockerNotAvailable"), L("Toast_DockerNotInstalled"));
                }
            }

            _logger?.LogInformation("Docker availability check: {IsAvailable}", IsDockerAvailable);

            if (IsDockerAvailable)
            {
                _ = ValidateDockerContainerNameAsync(showToast: false);
                _ = RefreshContainerStatusAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking Docker availability");
            IsDockerAvailable = false;
            if (showToast)
            {
                _toastService?.Error(L("Toast_CheckFailed"), L("Toast_CouldNotCheckDocker"));
            }
        }
        finally
        {
            IsDockerChecking = false;
        }
    }

    private async Task ValidateDockerContainerNameAsync(bool showToast)
    {
        if (!IsDockerAvailable) return;
        if (_dockerDeploymentService == null) return;

        var containerName = DockerContainerName.Trim();
        var composePathForValidation = DockerComposePath;
        if (string.IsNullOrWhiteSpace(containerName))
        {
            HasDockerContainerNameChecked = true;
            IsDockerContainerNameAvailable = false;
            return;
        }

        // LostFocus calls (showToast=false) are no-ops when the value hasn't changed,
        // and we've already checked it, avoiding redundant network calls.
        if (!showToast &&
            string.Equals(_lastValidatedContainerName, containerName, StringComparison.Ordinal) &&
            string.Equals(_lastValidatedContainerComposePath, composePathForValidation, StringComparison.Ordinal) &&
            HasDockerContainerNameChecked)
        {
            return;
        }

        _lastValidatedContainerName = containerName;
        _lastValidatedContainerComposePath = composePathForValidation;

        try
        {
            IsDockerContainerNameChecking = true;

            if (_dockerContainerNameCts != null)
            {
                await _dockerContainerNameCts.CancelAsync();
            }

            _dockerContainerNameCts = new CancellationTokenSource();

            var available = await _dockerDeploymentService.IsContainerNameAvailableAsync(containerName,
                _dockerContainerNameCts.Token);

            if (!available && !string.IsNullOrWhiteSpace(DockerComposePath))
            {
                var composeDirectory = Path.GetDirectoryName(DockerComposePath);
                if (!string.IsNullOrWhiteSpace(composeDirectory))
                {
                    var ownedByCurrentCompose = await _dockerDeploymentService.IsContainerOwnedByComposeAsync(
                        composeDirectory,
                        containerName,
                        _dockerContainerNameCts.Token);
                    if (ownedByCurrentCompose)
                    {
                        available = true;
                    }
                }
            }

            HasDockerContainerNameChecked = true;
            IsDockerContainerNameAvailable = available;

            if (showToast && !available)
            {
                _toastService?.Warning(L("Toast_CheckFailed"), L("Toast_CouldNotStartContainer"));
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error checking docker container name availability");
            HasDockerContainerNameChecked = true;
            IsDockerContainerNameAvailable = false;
        }
        finally
        {
            IsDockerContainerNameChecking = false;
        }
    }

    private async Task GenerateDockerComposeAsync()
    {
        // Back-compat: older UI used "Generate"; new UI uses "Save".
        await SaveDockerComposeAsync();
    }

    private async Task SaveDockerComposeAsync()
    {
        try
        {
            if (_serviceProvider == null || _dockerDeploymentService == null)
            {
                return;
            }

            var presetId = _presetService?.CurrentPreset?.Id;

            var targetComposePath = string.IsNullOrWhiteSpace(DockerComposePath)
                ? ResolveDefaultComposeFilePath(presetId)
                : DockerComposePath;

            var config = new FrpcDockerConfig
            {
                ImageName = DockerImageName,
                Tag = DockerImageTag,
                ConfigPath = "./frpc.toml",
                ContainerName = DockerContainerName,
                RestartPolicy = DockerRestartPolicy
            };

            var composePath = await _dockerDeploymentService.GenerateDockerComposeAsync(targetComposePath, config);
            DockerComposePath = composePath;

            _composeSnapshot = new DockerComposeSnapshot(
                DockerImageName,
                DockerImageTag,
                DockerContainerName,
                DockerRestartPolicy);
            IsDockerComposeDirty = false;

            _toastService?.Success(L("Toast_Generated"), L("Toast_DockerComposeSaved", composePath));
            _logger?.LogInformation("Saved docker-compose.yml at {Path}", composePath);

            if (IsDockerAvailable)
            {
                _ = ValidateDockerContainerNameAsync(showToast: false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving docker-compose.yml");
            _toastService?.Error(L("Toast_GenerationFailed"), L("Toast_CouldNotGenerateDockerCompose"));
        }
    }

    private async Task RefreshContainerStatusAsync()
    {
        try
        {
            if (!IsDockerAvailable || _dockerDeploymentService == null)
            {
                IsContainerRunning = false;
                return;
            }

            IsContainerRunning = await _dockerDeploymentService.IsContainerRunningAsync(DockerContainerName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error refreshing docker container status");
            IsContainerRunning = false;
        }
    }

    private async Task LoadDockerComposeFromFileAsync(string composePath)
    {
        try
        {
            var loadedImageName = DockerImageName;
            var loadedImageTag = DockerImageTag;
            var loadedContainerName = DockerContainerName;
            var loadedRestartPolicy = DockerRestartPolicy;

            var lines = await File.ReadAllLinesAsync(composePath);
            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (line.StartsWith("image:", StringComparison.OrdinalIgnoreCase))
                {
                    var image = line["image:".Length..].Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(image))
                    {
                        var lastSlash = image.LastIndexOf('/');
                        var lastColon = image.LastIndexOf(':');
                        if (lastColon > lastSlash)
                        {
                            loadedImageTag = image[(lastColon + 1)..];
                            loadedImageName = image[..lastColon];
                        }
                        else
                        {
                            loadedImageName = image;
                        }
                    }
                }

                if (line.StartsWith("container_name:", StringComparison.OrdinalIgnoreCase))
                {
                    var name = line["container_name:".Length..].Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        loadedContainerName = name;
                    }
                }

                if (!line.StartsWith("restart:", StringComparison.OrdinalIgnoreCase)) continue;
                var restart = line["restart:".Length..].Trim().Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(restart))
                {
                    loadedRestartPolicy = restart;
                }
            }

            _suppressComposeDirtyTracking = true;
            _suppressDockerAutoRefresh = true;
            _suppressDockerImageReset = true;
            DockerImageName = loadedImageName;
            DockerImageTag = loadedImageTag;
            if (!string.IsNullOrWhiteSpace(loadedImageTag))
            {
                _lastKnownDockerImageTag = loadedImageTag.Trim();
            }

            DockerContainerName = loadedContainerName;
            DockerRestartPolicy = loadedRestartPolicy;
            _suppressComposeDirtyTracking = false;
            _suppressDockerAutoRefresh = false;
            _suppressDockerImageReset = false;

            // Seed tag list so the version ComboBox can render the selected value immediately
            // (especially when switching presets/configs with different images).
            DockerImageTags = string.IsNullOrWhiteSpace(DockerImageTag) ? [] : [DockerImageTag];
            HasDockerImageChecked = false;
            IsDockerImageAvailable = false;

            HasDockerContainerNameChecked = false;
            IsDockerContainerNameAvailable = false;

            // Update last-validated cache so LostFocus immediately after load is a no-op.
            _lastValidatedContainerName = DockerContainerName;
            _lastValidatedImageName = DockerImageName;

            _composeSnapshot = new DockerComposeSnapshot(
                DockerImageName,
                DockerImageTag,
                DockerContainerName,
                DockerRestartPolicy);
            IsDockerComposeDirty = false;

            if (IsDockerMode)
            {
                _ = RefreshDockerImageTagsAsync(showToast: false);
            }

            if (IsDockerAvailable)
            {
                _ = ValidateDockerContainerNameAsync(showToast: false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load docker-compose.yml from {Path}", composePath);
        }
    }

    private async Task RefreshDockerImageTagsAsync(bool showToast)
    {
        if (_dockerDeploymentService == null) return;
        if (string.IsNullOrWhiteSpace(DockerImageName)) return;

        var imageName = DockerImageName.Trim();

        // LostFocus calls (showToast=false) are no-ops when the value hasn't changed,
        // and we've already checked it, avoiding redundant network calls.
        if (!showToast &&
            string.Equals(_lastValidatedImageName, imageName, StringComparison.Ordinal) &&
            HasDockerImageChecked)
        {
            return;
        }

        _lastValidatedImageName = imageName;

        var hasSuccessfulCheck = false;

        try
        {
            IsDockerImageTagsLoading = true;
            HasDockerImageChecked = false;
            IsDockerImageAvailable = false;

            if (_dockerTagsCts != null)
            {
                await _dockerTagsCts.CancelAsync();
            }

            _dockerTagsCts = new CancellationTokenSource();

            var tags = await _dockerDeploymentService.GetAvailableImageTagsAsync(DockerImageName, _dockerTagsCts.Token);
            var remoteTagList = tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .ToList();

            var configuredTagFromCompose = TryReadImageTagFromCompose(DockerComposePath);
            if (!string.IsNullOrWhiteSpace(configuredTagFromCompose) &&
                !string.Equals(DockerImageTag, configuredTagFromCompose, StringComparison.Ordinal))
            {
                var previousSuppressDirty = _suppressComposeDirtyTracking;
                _suppressComposeDirtyTracking = true;
                DockerImageTag = configuredTagFromCompose;
                _suppressComposeDirtyTracking = previousSuppressDirty;
            }

            var configuredTag = string.IsNullOrWhiteSpace(DockerImageTag) ? string.Empty : DockerImageTag.Trim();

            // Keep configured tag visible in ComboBox even when Docker Hub response doesn't include it.
            // This matches the restore flow behavior (seeded list retains configured version display).
            var displayTagList = remoteTagList.ToList();
            if (!string.IsNullOrWhiteSpace(configuredTag) &&
                !displayTagList.Contains(configuredTag, StringComparer.OrdinalIgnoreCase))
            {
                displayTagList.Insert(0, configuredTag);
            }

            DockerImageTags = displayTagList;

            // Available = the specific configured tag was found in the fetched list.
            // ComboBox SelectedItem needs exact item identity/value from ItemsSource to render text,
            // so normalize the current tag to the exact matched list item (case-preserving).
            var matchedTag = string.IsNullOrWhiteSpace(configuredTag)
                ? null
                : displayTagList.FirstOrDefault(tag =>
                    string.Equals(tag, configuredTag, StringComparison.OrdinalIgnoreCase));

            IsDockerImageAvailable = !string.IsNullOrWhiteSpace(configuredTag) &&
                                     remoteTagList.Contains(configuredTag, StringComparer.OrdinalIgnoreCase);

            // Avalonia ComboBox SelectedItem can appear empty when value text matches but the
            // SelectedItem reference is not the exact instance from ItemsSource.
            // Rebind to the matched instance from tagList to guarantee visible selection.
            if (matchedTag != null && !ReferenceEquals(DockerImageTag, matchedTag))
            {
                var previousSuppressDirty = _suppressComposeDirtyTracking;
                _suppressComposeDirtyTracking = true;
                DockerImageTag = matchedTag;
                _suppressComposeDirtyTracking = previousSuppressDirty;
            }

            hasSuccessfulCheck = true;
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error refreshing docker image tags");
            // Keep current values on refresh failure to avoid clearing selected version on first load.
            IsDockerImageAvailable = false;
            if (showToast)
            {
                _toastService?.Warning(L("Toast_CheckFailed"), L("Toast_CouldNotFetchVersions"));
            }
        }
        finally
        {
            IsDockerImageTagsLoading = false;
            HasDockerImageChecked = hasSuccessfulCheck;
            if (hasSuccessfulCheck)
            {
                ScheduleDockerImageTagReselect();
            }
        }
    }

    private void ScheduleDockerImageTagReselect()
    {
        if (_pendingDockerImageTagResync)
        {
            return;
        }

        _pendingDockerImageTagResync = true;
        Dispatcher.UIThread.Post(() =>
        {
            ApplyDockerImageTagReselect();

            // Some ComboBox state updates happen one frame later after ItemsSource refresh.
            // Re-apply once more to guarantee final selected state.
            Dispatcher.UIThread.Post(() =>
            {
                ApplyDockerImageTagReselect();
                _pendingDockerImageTagResync = false;
            }, DispatcherPriority.Background);
        }, DispatcherPriority.Loaded);
    }

    private void ApplyDockerImageTagReselect()
    {
        if (DockerImageTags.Count == 0)
        {
            return;
        }

        var tag = string.IsNullOrWhiteSpace(DockerImageTag)
            ? _lastKnownDockerImageTag
            : DockerImageTag;

        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var index = DockerImageTags.FindIndex(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            index = 0;
        }

        _suppressDockerImageTagIndexSync = true;
        try
        {
            // Force UI to re-apply selection even if index value is unchanged.
            DockerImageTagSelectedIndex = -1;
            DockerImageTagSelectedIndex = index;
        }
        finally
        {
            _suppressDockerImageTagIndexSync = false;
        }
    }

    private static string? TryReadImageTagFromCompose(string composePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(composePath) || !File.Exists(composePath))
            {
                return null;
            }

            foreach (var raw in File.ReadLines(composePath))
            {
                var line = raw.Trim();
                if (!line.StartsWith("image:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var image = line["image:".Length..].Trim().Trim('"', '\'');
                if (string.IsNullOrWhiteSpace(image))
                {
                    return null;
                }

                var lastSlash = image.LastIndexOf('/');
                var lastColon = image.LastIndexOf(':');
                if (lastColon > lastSlash && lastColon < image.Length - 1)
                {
                    return image[(lastColon + 1)..];
                }

                return null;
            }
        }
        catch
        {
            // ignore parsing errors and fall back to current in-memory value.
        }

        return null;
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

            if (_dockerDeploymentService != null &&
                !(IsDockerAvailable = await _dockerDeploymentService.IsDockerAvailableAsync()))
            {
                _toastService?.Error(L("Toast_DockerNotAvailable"), L("Toast_DockerNotInstalled"));
                return;
            }

            if (IsContainerRunning)
            {
                _toastService?.Info(L("RecreateContainer"), L("Toast_RecreatingContainer"));
                var recreateSuccess = await _dockerDeploymentService!.RecreateDockerComposeAsync(composeDirectory);
                if (recreateSuccess)
                {
                    _toastService?.Success(L("Toast_ContainerStarted"), L("Toast_DockerContainerRunning"));
                    if (_dockerDeploymentService != null)
                    {
                        IsContainerRunning = await _dockerDeploymentService.IsContainerRunningAsync(
                            DockerContainerName);
                    }

                    return;
                }

                _toastService?.Error(L("Toast_StartFailed"), L("Toast_CouldNotStartContainer"));
                return;
            }

            _toastService?.Info(L("StartContainer"), L("Toast_StartingContainer"));

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
            if (!IsContainerRunning)
            {
                return;
            }

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

            _toastService?.Info(L("StopContainer"), L("Toast_StoppingContainer"));

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

    private async Task LoadFromPresetAsync(ConfigPreset preset)
    {
        _suppressDockerAutoRefresh = true;
        _suppressDockerImageReset = true;
        _suppressComposeAutoLoad = true;
        try
        {
            var settings = preset.Deployment;

            FrpcBinaryPath = settings.FrpcBinaryPath ?? "";

            // Reset Docker validation state.
            HasDockerContainerNameChecked = false;
            IsDockerContainerNameAvailable = false;
            HasDockerImageChecked = false;
            IsDockerImageAvailable = false;

            // Set sensible defaults — will be overwritten below if compose file exists.
            DockerContainerName = GenerateDefaultContainerName(preset.Name);
            DockerImageName = "fatedier/frpc";
            DockerImageTag = "latest";
            DockerImageTags = ["latest"];
            DockerRestartPolicy = "unless-stopped";

            // Clear last-validated cache so next user edit triggers a fresh check.
            _lastValidatedContainerName = DockerContainerName;
            _lastValidatedImageName = DockerImageName;

            var presetFolder = GetPresetFolderPath(preset.Id);
            var composePath = Path.Combine(presetFolder, "docker-compose.yml");

            // Load docker-compose.yml directly (awaited) so values are ready before the UI renders.
            if (File.Exists(composePath))
            {
                await LoadDockerComposeFromFileAsync(composePath);
            }

            // Set DockerComposePath after loading so OnDockerComposePathChanged only manages
            // snapshot/dirty state, not file loading (suppressed above).
            DockerComposePath = composePath;

            ServiceScopeValue = settings.ServiceScope;
            AutoStartOnBoot = settings.AutoStartOnBoot;
            ServiceEnabled = settings.ServiceEnabled;

            // Set mode last so the Docker UI becomes visible only after fields are ready.
            SelectedDeploymentMode = settings.DeploymentMode;
        }
        finally
        {
            _suppressDockerAutoRefresh = false;
            _suppressDockerImageReset = false;
            _suppressComposeAutoLoad = false;
        }

        if (!IsDockerMode) return;
        _ = CheckDockerAsync(showToast: false);
        // RefreshDockerImageTagsAsync is always called here after SelectedDeploymentMode is set
        // (IsDockerMode = true). The call inside LoadDockerComposeFromFileAsync is skipped during
        // preset load because IsDockerMode is still false at that moment (mode is set after the awaiting).
        // HasDockerImageChecked=false ensures the LostFocus gate doesn't suppress this.
        _ = RefreshDockerImageTagsAsync(showToast: false);
    }

    private void SaveToPreset(ConfigPreset preset)
    {
        var settings = preset.Deployment;

        settings.DeploymentMode = SelectedDeploymentMode;
        settings.FrpcBinaryPath = FrpcBinaryPath;

        // Docker fields (image, tag, container name, restart policy) are stored exclusively
        // in docker-compose.yml and are never persisted to the preset.

        settings.ServiceScope = ServiceScopeValue;
        settings.AutoStartOnBoot = AutoStartOnBoot;
        settings.ServiceEnabled = ServiceEnabled;
    }

    #endregion

    private static string GetPresetFolderPath(Guid presetId)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "FrapaClonia", "presets", presetId.ToString("N"));
    }

    private static string GenerateDefaultContainerName(string presetName)
    {
        var value = presetName.Trim().ToLowerInvariant();
        value = Regex.Replace(value, "[^a-z0-9]+", "-");
        value = value.Trim('-');
        return string.IsNullOrWhiteSpace(value) ? "frapa-clonia-frpc" : $"frapa-clonia-{value}";
    }

    private void UpdateDockerComposeDirtyFlag()
    {
        if (_suppressComposeDirtyTracking) return;

        if (_composeSnapshot == null)
        {
            IsDockerComposeDirty = false;
            return;
        }

        IsDockerComposeDirty =
            !string.Equals(_composeSnapshot.ImageName, DockerImageName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_composeSnapshot.ImageTag, DockerImageTag, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_composeSnapshot.ContainerName, DockerContainerName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_composeSnapshot.RestartPolicy, DockerRestartPolicy, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDefaultComposeFilePath(Guid? presetId)
    {
        if (presetId.HasValue)
        {
            var dir = GetPresetFolderPath(presetId.Value);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "docker-compose.yml");
        }

        var downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "frapa-clonia-docker");
        Directory.CreateDirectory(downloadsDir);
        return Path.Combine(downloadsDir, "docker-compose.yml");
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
}