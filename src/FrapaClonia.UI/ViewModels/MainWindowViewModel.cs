using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using System.Reflection;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// Main window view model
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILogger<MainWindowViewModel>? _logger;
    private readonly NavigationService? _navigation;

    [ObservableProperty] private Control? _currentView;

    public static string Version
    {
        get
        {
            var informationalVersion = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrEmpty(informationalVersion))
                return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

#if DEBUG
            // Debug: Show full version with build metadata
            return informationalVersion;
#else
            // Release: Strip build metadata (everything after '+')
            var plusIndex = informationalVersion.IndexOf('+');
            return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
#endif
        }
    }

    public static string Copyright => "© 2025 Robert He";

    public IRelayCommand NavigateToDashboardCommand { get; }
    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxiesCommand { get; }
    public IRelayCommand NavigateToVisitorsCommand { get; }
    public IRelayCommand NavigateToDeploymentCommand { get; }
    public IRelayCommand NavigateToLogsCommand { get; }
    public IRelayCommand NavigateToSettingsCommand { get; }

    // Default constructor for design-time support
    public MainWindowViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<MainWindowViewModel>.Instance,
        null!)
    {
    }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        NavigationService navigation)
    {
        _logger = logger;
        _navigation = navigation;

        NavigateToDashboardCommand = new RelayCommand(() => Navigate("dashboard"));
        NavigateToServerConfigCommand = new RelayCommand(() => Navigate("server"));
        NavigateToProxiesCommand = new RelayCommand(() => Navigate("proxies"));
        NavigateToVisitorsCommand = new RelayCommand(() => Navigate("visitors"));
        NavigateToDeploymentCommand = new RelayCommand(() => Navigate("deployment"));
        NavigateToLogsCommand = new RelayCommand(() => Navigate("logs"));
        NavigateToSettingsCommand = new RelayCommand(() => Navigate("settings"));

        // Subscribe to navigation changes
        _navigation.PageChanged += (_, _) => { CurrentView = _navigation.CurrentView; };

        // Initialize with dashboard
        Navigate("dashboard");

        _logger.LogInformation("MainWindowViewModel initialized");
    }

    private void Navigate(string page)
    {
        _navigation?.NavigateTo(page);
        // _logger?.LogInformation("Navigated to: {Page}", page);
    }
}