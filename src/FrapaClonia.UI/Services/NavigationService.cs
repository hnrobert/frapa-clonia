using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using FrapaClonia.UI.Views;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Simple navigation service for switching between views
/// </summary>
public class NavigationService : ObservableObject
{
    private Control? _currentView;
    private string _currentPage = "dashboard";

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

        CurrentView = page switch
        {
            "dashboard" => new DashboardView(),
            "server" => new ServerConfigView(),
            "proxies" => new ProxyListView(),
            "visitors" => new VisitorListView(),
            "deployment" => new DeploymentView(),
            "logs" => new LogsView(),
            "settings" => new SettingsView(),
            _ => null
        };

        PageChanged?.Invoke(this, EventArgs.Empty);
    }
}
