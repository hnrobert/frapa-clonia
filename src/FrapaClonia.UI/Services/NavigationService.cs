using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using FrapaClonia.UI.Views;
using FrapaClonia.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Simple navigation service for switching between views
/// </summary>
public class NavigationService : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private Control? _currentView;
    private string _currentPage = "dashboard";

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Control? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public string CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public event EventHandler? PageChanged;

    public void NavigateTo(string page)
    {
        CurrentPage = page;

        Control view = page switch
        {
            "dashboard" => new DashboardView(),
            "server" => new ServerConfigView(),
            "proxies" => new ProxyListView(),
            "visitors" => new VisitorListView(),
            "deployment" => new DeploymentView(),
            "logs" => new LogsView(),
            "settings" => new SettingsView(),
            _ => new DashboardView() // Default fallback
        };

        // Set the DataContext from the service provider
        view.DataContext = page switch
        {
            "dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
            "server" => _serviceProvider.GetRequiredService<ServerConfigViewModel>(),
            "proxies" => _serviceProvider.GetRequiredService<ProxyListViewModel>(),
            "visitors" => _serviceProvider.GetRequiredService<VisitorListViewModel>(),
            "deployment" => _serviceProvider.GetRequiredService<DeploymentViewModel>(),
            "logs" => _serviceProvider.GetRequiredService<LogsViewModel>(),
            "settings" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
            _ => _serviceProvider.GetRequiredService<DashboardViewModel>()
        };

        CurrentView = view;
        PageChanged?.Invoke(this, EventArgs.Empty);
    }
}
