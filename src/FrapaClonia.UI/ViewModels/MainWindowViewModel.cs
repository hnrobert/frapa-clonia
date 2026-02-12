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

    private const double MinSidebarWidth = 190;
    private const double MaxSidebarWidth = 280;

    [ObservableProperty] private Control? _currentView;
    [ObservableProperty] private string _currentPage = "dashboard";
    [ObservableProperty] private double _sidebarWidth = MinSidebarWidth;

    /// <summary>
    /// The toast notification service
    /// </summary>
    public ToastService? ToastService { get; private set; }

    // Active state properties for navigation
    public bool IsDashboardActive => CurrentPage == "dashboard";
    public bool IsServerActive => CurrentPage == "server";
    public bool IsProxiesActive => CurrentPage == "proxies";
    public bool IsVisitorsActive => CurrentPage == "visitors";
    public bool IsDeploymentActive => CurrentPage == "deployment";
    public bool IsLogsActive => CurrentPage == "logs";
    public bool IsSettingsActive => CurrentPage == "settings";

    partial void OnCurrentPageChanged(string value)
    {
        _ = value; // Discard parameter without warning
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsServerActive));
        OnPropertyChanged(nameof(IsProxiesActive));
        OnPropertyChanged(nameof(IsVisitorsActive));
        OnPropertyChanged(nameof(IsDeploymentActive));
        OnPropertyChanged(nameof(IsLogsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    partial void OnSidebarWidthChanged(double value)
    {
        SidebarWidth = value switch
        {
            // Clamp width to min/max bounds
            < MinSidebarWidth => MinSidebarWidth,
            > MaxSidebarWidth => MaxSidebarWidth,
            _ => value
        };
    }

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
        null!,
        null!)
    {
    }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        NavigationService navigation,
        ToastService? toastService)
    {
        _logger = logger;
        _navigation = navigation;
        ToastService = toastService;

        NavigateToDashboardCommand = new RelayCommand(() => Navigate("dashboard"));
        NavigateToServerConfigCommand = new RelayCommand(() => Navigate("server"));
        NavigateToProxiesCommand = new RelayCommand(() => Navigate("proxies"));
        NavigateToVisitorsCommand = new RelayCommand(() => Navigate("visitors"));
        NavigateToDeploymentCommand = new RelayCommand(() => Navigate("deployment"));
        NavigateToLogsCommand = new RelayCommand(() => Navigate("logs"));
        NavigateToSettingsCommand = new RelayCommand(() => Navigate("settings"));

        // Subscribe to navigation changes
        _navigation.PageChanged += (_, _) =>
        {
            CurrentView = _navigation.CurrentView;
            CurrentPage = _navigation.CurrentPage;
        };

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