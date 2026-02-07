using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using System.Text.Json;
// ReSharper disable UnusedParameterInPartialMethod

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for proxy list management
/// </summary>
public partial class ProxyListViewModel : ObservableObject
{
    private readonly ILogger<ProxyListViewModel> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private List<ProxyConfig> _proxies = [];

    [ObservableProperty]
    private ProxyConfig? _selectedProxy;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    public IRelayCommand AddProxyCommand { get; }
    public IRelayCommand EditProxyCommand { get; }
    public IRelayCommand DeleteProxyCommand { get; }
    public IRelayCommand DuplicateProxyCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand ExportAllCommand { get; }
    public IRelayCommand ImportCommand { get; }
    public IRelayCommand ClearAllCommand { get; }

    public List<string> ProxyTypes { get; } = ["All", "tcp", "udp", "http", "https", "stcp", "xtcp", "sudp", "tcpmux"];

    public ProxyListViewModel(
        ILogger<ProxyListViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;
        _serviceProvider = serviceProvider;

        AddProxyCommand = new RelayCommand(AddProxy);
        EditProxyCommand = new RelayCommand(EditProxy, () => SelectedProxy != null);
        DeleteProxyCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DeleteProxyAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting proxy");
            }
        }, () => SelectedProxy != null);
        DuplicateProxyCommand = new RelayCommand(DuplicateProxy, () => SelectedProxy != null);
        RefreshCommand = new RelayCommand(async void () =>
        {
            try
            {
                await LoadProxiesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error loading proxies");
            }
        });
        ExportAllCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ExportAllAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error exporting all proxies");
            }
        });
        ImportCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ImportAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error importing proxies");
            }
        });
        ClearAllCommand = new RelayCommand(async void () =>
        {
            try
            {
                await ClearAllAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting all proxies");
            }
        }, () => Proxies.Count > 0);

        _ = Task.Run(LoadProxiesAsync);
    }

    partial void OnSelectedProxyChanged(ProxyConfig? value)
    {
        EditProxyCommand.NotifyCanExecuteChanged();
        DeleteProxyCommand.NotifyCanExecuteChanged();
        DuplicateProxyCommand.NotifyCanExecuteChanged();
    }

    partial void OnProxiesChanged(List<ProxyConfig> value)
    {
        ClearAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterProxies();
    }

    partial void OnFilterTypeChanged(string value)
    {
        FilterProxies();
    }

    private void FilterProxies()
    {
        // Filtering is implemented in LoadProxiesAsync
        _ = Task.Run(LoadProxiesAsync);
    }

    public async Task LoadProxiesAsync()
    {
        try
        {
            IsLoading = true;

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                var allProxies = config.Proxies;

                // Apply filters
                var filtered = allProxies.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var query = SearchQuery.ToLower();
                    filtered = filtered.Where(p =>
                        p.Name.ToLower().Contains(query) ||
                        p.Type.ToLower().Contains(query));
                }

                if (FilterType != "All")
                {
                    filtered = filtered.Where(p => p.Type == FilterType);
                }

                Proxies = filtered.ToList();

                _logger.LogInformation("Loaded {Count} proxies", Proxies.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading proxies");
            Proxies = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void AddProxy()
    {
        _logger.LogInformation("Add proxy clicked");

        // Create new proxy and show editor dialog
        var newProxy = new ProxyConfig();
        var editorLogger = _serviceProvider.GetRequiredService<ILogger<ProxyEditorViewModel>>();
        var viewModel = new ProxyEditorViewModel(editorLogger, _configurationService, _validationService, newProxy);

        var editorWindow = new ProxyEditorView
        {
            DataContext = viewModel
        };

        if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                // User clicked Save - refresh the list
                _ = Task.Run(LoadProxiesAsync);
            }
        }
    }

    private async void EditProxy()
    {
        if (SelectedProxy == null) return;
        _logger.LogInformation("Edit proxy: {ProxyName}", SelectedProxy.Name);

        // Clone the proxy to avoid modifying the original until saved
        var proxyClone = JsonSerializer.Deserialize<ProxyConfig>(
            JsonSerializer.Serialize(SelectedProxy),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (proxyClone != null)
        {
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<ProxyEditorViewModel>>();
            var viewModel = new ProxyEditorViewModel(editorLogger, _configurationService, _validationService, proxyClone);

            var editorWindow = new ProxyEditorView
            {
                DataContext = viewModel
            };

            if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
                if (result == true)
                {
                    // User clicked Save - refresh the list
                    _ = Task.Run(LoadProxiesAsync);
                }
            }
        }
    }

    private async Task DeleteProxyAsync()
    {
        if (SelectedProxy == null) return;

        _logger.LogInformation("Delete proxy: {ProxyName}", SelectedProxy.Name);

        try
        {
            IsSaving = true;

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                config.Proxies.RemoveAll(p => p.Name == SelectedProxy.Name);
                await _configurationService.SaveConfigurationAsync(configPath, config);

                await LoadProxiesAsync();
                SelectedProxy = null;

                _logger.LogInformation("Proxy deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting proxy");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void DuplicateProxy()
    {
        if (SelectedProxy == null) return;

        _logger.LogInformation("Duplicate proxy: {ProxyName}", SelectedProxy.Name);

        var newProxy = new ProxyConfig
        {
            Name = $"{SelectedProxy.Name} (Copy)",
            Type = SelectedProxy.Type,
            LocalIP = SelectedProxy.LocalIP,
            LocalPort = SelectedProxy.LocalPort,
            RemotePort = SelectedProxy.RemotePort,
            CustomDomains = SelectedProxy.CustomDomains,
            Subdomain = SelectedProxy.Subdomain,
            Transport = SelectedProxy.Transport,
            HealthCheck = SelectedProxy.HealthCheck,
            Plugin = SelectedProxy.Plugin,
            // Copy other properties as needed
        };

        Proxies.Add(newProxy);

        _logger.LogInformation("Proxy duplicated: {NewProxyName}", newProxy.Name);
    }

    private async Task ExportAllAsync()
    {
        _logger.LogInformation("Export all proxies");

        try
        {
            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider == null) return;

            // Create save file dialog
            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Proxies",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                var options = new JsonSerializerOptions { WriteIndented = true };
                await JsonSerializer.SerializeAsync(stream, Proxies, options);
                _logger.LogInformation("Exported {Count} proxies to {FilePath}", Proxies.Count, file.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting proxies");
        }
    }

    private async Task ImportAsync()
    {
        _logger.LogInformation("Import proxies");

        try
        {
            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var storageProvider = desktop.MainWindow?.StorageProvider;
            if (storageProvider == null) return;

            // Create open file dialog
            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Import Proxies",
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                    {
                        Patterns = new[] { "*.json" }
                    },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" }
                    }
                },
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                var importedProxies = await JsonSerializer.DeserializeAsync<List<ProxyConfig>>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (importedProxies != null)
                {
                    // Save imported proxies to configuration
                    var configPath = _configurationService.GetDefaultConfigPath();
                    var config = await _configurationService.LoadConfigurationAsync(configPath);

                    if (config != null)
                    {
                        config.Proxies.AddRange(importedProxies);
                        await _configurationService.SaveConfigurationAsync(configPath, config);
                        await LoadProxiesAsync();
                        _logger.LogInformation("Imported {Count} proxies from {FilePath}", importedProxies.Count, file.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing proxies");
        }
    }

    private async Task ClearAllAsync()
    {
        _logger.LogInformation("Clear all proxies");

        try
        {
            IsSaving = true;

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                config.Proxies.Clear();
                await _configurationService.SaveConfigurationAsync(configPath, config);

                await LoadProxiesAsync();

                _logger.LogInformation("All proxies cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing proxies");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
