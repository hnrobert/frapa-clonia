using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// Main window view model
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly NavigationService _navigation;

    [ObservableProperty]
    private Control? _currentView;

    public IRelayCommand NavigateToDashboardCommand { get; }
    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxiesCommand { get; }
    public IRelayCommand NavigateToVisitorsCommand { get; }
    public IRelayCommand NavigateToSettingsCommand { get; }

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
        NavigateToSettingsCommand = new RelayCommand(() => Navigate("settings"));

        // Subscribe to navigation changes
        _navigation.PageChanged += (_, _) =>
        {
            CurrentView = _navigation.CurrentView;
        };

        // Initialize with dashboard
        Navigate("dashboard");

        _logger.LogInformation("MainWindowViewModel initialized");
    }

    private void Navigate(string page)
    {
        _navigation.NavigateTo(page);
        _logger.LogInformation("Navigated to: {Page}", page);
    }
}
