using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.Utils;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using FrapaClonia.Shared.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable UnusedParameterInPartialMethod

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for proxy list management
/// </summary>
public partial class ProxyListViewModel : ObservableObject
{
    private readonly ILogger<ProxyListViewModel>? _logger;
    private readonly IValidationService? _validationService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ToastService? _toastService;
    private readonly IPresetService? _presetService;

    [ObservableProperty] private List<ProxyConfig> _proxies = [];

    [ObservableProperty] private ProxyConfig? _selectedProxy;

    [ObservableProperty] private string _searchQuery = "";

    [ObservableProperty] private string _filterType = "All";

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private bool _isSaving;

    public IRelayCommand AddProxyCommand { get; }
    public IRelayCommand EditProxyCommand { get; }
    public IRelayCommand DeleteProxyCommand { get; }
    public IRelayCommand DuplicateProxyCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public IRelayCommand ClearAllCommand { get; }

    // ReSharper disable once UnusedMember.Global
    public List<string> ProxyTypes { get; } = ["All", "tcp", "udp", "http", "https", "stcp", "xtcp", "sudp", "tcpmux"];

    // Default constructor for design-time support
    public ProxyListViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<ProxyListViewModel>.Instance,
        null!,
        null!,
        null!,
        null!)
    {
    }

    public ProxyListViewModel(
        ILogger<ProxyListViewModel> logger,
        IValidationService validationService,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        IPresetService presetService)
    {
        _logger = logger;
        _validationService = validationService;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _presetService = presetService;

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
                _logger?.LogError(e, "Error deleting proxy");
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
                _logger?.LogError(e, "Error loading proxies");
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
                _logger?.LogError(e, "Error deleting all proxies");
            }
        }, () => Proxies.Count > 0);

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
        }

        // Note: Loading is initiated by the View's OnLoaded event
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        // Reload proxies when preset changes
        _ = LoadProxiesAsync();
    }

    public void Initialize()
    {
        _ = LoadProxiesAsync();
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
        _ = LoadProxiesAsync();
    }

    private Task LoadProxiesAsync()
    {
        try
        {
            IsLoading = true;
            _logger?.LogInformation("LoadProxiesAsync: Starting, IsLoading={IsLoading}", IsLoading);

            if (_presetService?.CurrentPreset != null)
            {
                var allProxies = _presetService.CurrentPreset.Configuration.Proxies;
                var filtered = allProxies.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var query = SearchQuery.ToLower();
                    filtered = filtered.Where(p =>
                        p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                        p.Type.Contains(query, StringComparison.CurrentCultureIgnoreCase));
                }

                if (FilterType != "All")
                {
                    filtered = filtered.Where(p => p.Type == FilterType);
                }

                Proxies = filtered.ToList();
                _logger?.LogInformation("Loaded {Count} proxies", Proxies.Count);
            }
            else
            {
                Proxies = [];
                _logger?.LogInformation("No current preset, setting Proxies to empty list");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading proxies");
            Proxies = [];
        }
        finally
        {
            IsLoading = false;
            _logger?.LogInformation("LoadProxiesAsync: Completed, IsLoading={IsLoading}", IsLoading);
        }

        return Task.CompletedTask;
    }

    private async void AddProxy()
    {
        try
        {
            _logger?.LogInformation("Add proxy clicked");

            if (WindowReuse.ActivateExisting<ProxyEditorView>() != null)
            {
                return;
            }

            if (_presetService?.CurrentPreset == null)
            {
                _toastService?.Error("Error", "No active preset");
                return;
            }

            // Create new proxy and show editor dialog
            var newProxy = new ProxyConfig();
            if (_serviceProvider == null) return;

            var editorLogger = _serviceProvider.GetRequiredService<ILogger<ProxyEditorViewModel>>();
            if (_validationService == null) return;

            var viewModel =
                new ProxyEditorViewModel(editorLogger, _presetService, _validationService, _toastService, newProxy);

            var editorWindow = new ProxyEditorView
            {
                DataContext = viewModel
            };

            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
                return;
            if (desktop.MainWindow == null) return;
            var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                // User clicked Save - refresh the list
                _ = LoadProxiesAsync();
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error adding proxy");
        }
    }

    private async void EditProxy()
    {
        try
        {
            if (SelectedProxy == null) return;
            _logger?.LogInformation("Edit proxy: {ProxyName}", SelectedProxy.Name);

            if (WindowReuse.ActivateExisting<ProxyEditorView>() != null)
            {
                return;
            }

            if (_presetService?.CurrentPreset == null)
            {
                _toastService?.Error("Error", "No active preset");
                return;
            }

            // Clone the proxy manually to avoid modifying the original until saved
            var proxyClone = CloneProxy(SelectedProxy);

            if (_serviceProvider == null) return;
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<ProxyEditorViewModel>>();

            if (_validationService == null) return;

            var viewModel =
                new ProxyEditorViewModel(editorLogger, _presetService, _validationService, _toastService, proxyClone);

            var editorWindow = new ProxyEditorView
            {
                DataContext = viewModel
            };

            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
                return;
            if (desktop.MainWindow == null) return;
            var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                // User clicked Save - refresh the list
                _ = LoadProxiesAsync();
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error editing proxy");
        }
    }

    private static ProxyConfig CloneProxy(ProxyConfig source)
    {
        return new ProxyConfig
        {
            Name = source.Name,
            Type = source.Type,
            Annotations = source.Annotations?.ToDictionary(kv => kv.Key, kv => kv.Value),
            Transport = source.Transport != null
                ? new ProxyTransport
                {
                    UseEncryption = source.Transport.UseEncryption,
                    UseCompression = source.Transport.UseCompression,
                    BandwidthLimit = source.Transport.BandwidthLimit,
                    BandwidthLimitMode = source.Transport.BandwidthLimitMode,
                    ProxyProtocolVersion = source.Transport.ProxyProtocolVersion
                }
                : null,
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            LoadBalancer = source.LoadBalancer,
            HealthCheck = source.HealthCheck,
            LocalIP = source.LocalIP,
            LocalPort = source.LocalPort,
            Plugin = source.Plugin,
            RemotePort = source.RemotePort,
            CustomDomains = source.CustomDomains?.ToList(),
            Subdomain = source.Subdomain,
            Locations = source.Locations?.ToList(),
            HttpUser = source.HttpUser,
            HttpPassword = source.HttpPassword,
            HostHeaderRewrite = source.HostHeaderRewrite,
            RequestHeaders = source.RequestHeaders,
            ResponseHeaders = source.ResponseHeaders,
            RouteByHttpUser = source.RouteByHttpUser,
            SecretKey = source.SecretKey,
            AllowUsers = source.AllowUsers?.ToList(),
            NatTraversal = source.NatTraversal,
            Multiplexer = source.Multiplexer
        };
    }

    private async Task DeleteProxyAsync()
    {
        if (SelectedProxy == null) return;
        if (_presetService?.CurrentPreset == null) return;

        _logger?.LogInformation("Delete proxy: {ProxyName}", SelectedProxy.Name);

        try
        {
            IsSaving = true;

            _presetService.CurrentPreset.Configuration.Proxies.RemoveAll(p => p.Name == SelectedProxy.Name);
            await _presetService.SaveCurrentPresetAsync();

            await LoadProxiesAsync();
            SelectedProxy = null;

            _logger?.LogInformation("Proxy deleted successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting proxy");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async void DuplicateProxy()
    {
        try
        {
            if (SelectedProxy == null) return;
            if (_presetService?.CurrentPreset == null) return;

            _logger?.LogInformation("Duplicate proxy: {ProxyName}", SelectedProxy.Name);

            try
            {
                IsSaving = true;

                var newProxy = CloneProxy(SelectedProxy);
                newProxy.Name = $"{SelectedProxy.Name} (Copy)";

                _presetService.CurrentPreset.Configuration.Proxies.Add(newProxy);
                await _presetService.SaveCurrentPresetAsync();

                await LoadProxiesAsync();

                _logger?.LogInformation("Proxy duplicated: {NewProxyName}", newProxy.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error duplicating proxy");
            }
            finally
            {
                IsSaving = false;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error in DuplicateProxy");
        }
    }

    private async Task ClearAllAsync()
    {
        _logger?.LogInformation("Clear all proxies");

        try
        {
            IsSaving = true;

            if (_presetService?.CurrentPreset != null)
            {
                _presetService.CurrentPreset.Configuration.Proxies.Clear();
                await _presetService.SaveCurrentPresetAsync();

                await LoadProxiesAsync();

                _logger?.LogInformation("All proxies cleared");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing proxies");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
