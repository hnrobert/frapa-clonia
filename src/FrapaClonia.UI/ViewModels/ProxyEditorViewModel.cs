using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for editing a single proxy configuration
/// </summary>
public partial class ProxyEditorViewModel : ObservableObject
{
    private readonly ILogger<ProxyEditorViewModel> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;
    private readonly ProxyConfig? _originalProxy;

    [ObservableProperty]
    private string _proxyName = "";

    [ObservableProperty]
    private string _proxyType = "tcp";

    [ObservableProperty]
    private string _localIP = "127.0.0.1";

    [ObservableProperty]
    private int _localPort;

    [ObservableProperty]
    private int? _remotePort;

    [ObservableProperty]
    private string? _customDomains;

    [ObservableProperty]
    private string? _subdomain;

    [ObservableProperty]
    private List<string>? _locations;

    [ObservableProperty]
    private string? _httpUser;

    [ObservableProperty]
    private string? _httpPassword;

    [ObservableProperty]
    private string? _hostHeaderRewrite;

    [ObservableProperty]
    private string? _requestHeaders;

    [ObservableProperty]
    private string? _responseHeaders;

    [ObservableProperty]
    private string? _secretKey;  // For STCP, XTCP, SUDP

    [ObservableProperty]
    private List<string>? _allowUsers;  // For STCP, XTCP, SUDP

    [ObservableProperty]
    private string? _multiplexer;  // For TCPMUX

    [ObservableProperty]
    private bool _useEncryption;

    [ObservableProperty]
    private bool _useCompression;

    [ObservableProperty]
    private string? _bandwidthLimit;

    [ObservableProperty]
    private string? _bandwidthLimitMode;

    [ObservableProperty]
    private bool _healthCheckEnabled;

    [ObservableProperty]
    private string _healthCheckType = "tcp";

    [ObservableProperty]
    private int _healthCheckTimeoutSeconds = 3;

    [ObservableProperty]
    private int _healthCheckMaxFailed = 3;

    [ObservableProperty]
    private int _healthCheckIntervalSeconds = 10;

    [ObservableProperty]
    private string? _healthCheckPath = "/";

    [ObservableProperty]
    private string? _healthCheckHeaders;

    [ObservableProperty]
    private string? _pluginType;

    [ObservableProperty]
    private string? _pluginHttpProxyUrl;

    [ObservableProperty]
    private string? _pluginSocks5Url;

    [ObservableProperty]
    private string? _pluginStaticFilePath;

    [ObservableProperty]
    private string? _pluginStaticFilePrefixUrl;

    [ObservableProperty]
    private string? _pluginHttps2HttpLocalAddr;

    [ObservableProperty]
    private string? _pluginHttps2HttpCrtPath;

    [ObservableProperty]
    private string? _pluginHttps2HttpKeyPath;

    [ObservableProperty]
    private string? _pluginHttp2HttpsLocalAddr;

    [ObservableProperty]
    private string? _pluginHttp2HttpsCrtPath;

    [ObservableProperty]
    private string? _pluginHttp2HttpsKeyPath;

    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isSaving;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand AddLocationCommand { get; }
    public IRelayCommand AddAllowUserCommand { get; }

    public List<string> ProxyTypes { get; } = ["tcp", "udp", "http", "https", "stcp", "xtcp", "sudp", "tcpmux"];

    public List<string> HealthCheckTypes { get; } = ["tcp", "http"];

    public List<string> BandwidthLimitModes { get; } = ["client", "server"];

    public List<string> PluginTypes { get; } = ["http_proxy", "socks5", "static_file", "https2http", "http2https"];

    public bool IsTcpOrUdp => ProxyType is "tcp" or "udp";
    public bool IsHttpOrHttps => ProxyType is "http" or "https";
    public bool IsStcpOrXtcpOrSudp => ProxyType is "stcp" or "xtcp" or "sudp";
    public bool IsTcpmux => ProxyType == "tcpmux";
    public bool NeedsRemotePort => IsTcpOrUdp;
    public bool NeedsDomain => IsHttpOrHttps || IsTcpmux;
    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);

    public ProxyEditorViewModel(
        ILogger<ProxyEditorViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService,
        ProxyConfig? proxyToEdit = null)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;
        _originalProxy = proxyToEdit;

        SaveCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving proxy");
            }
        }, () => !IsSaving);
        CancelCommand = new RelayCommand(() => _logger.LogInformation("Cancel edit"));
        AddLocationCommand = new RelayCommand(AddLocation);
        AddAllowUserCommand = new RelayCommand(AddAllowUser);

        if (proxyToEdit != null)
        {
            LoadFromProxy(proxyToEdit);
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnProxyTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsTcpOrUdp));
        OnPropertyChanged(nameof(IsHttpOrHttps));
        OnPropertyChanged(nameof(IsStcpOrXtcpOrSudp));
        OnPropertyChanged(nameof(IsTcpmux));
        OnPropertyChanged(nameof(NeedsRemotePort));
        OnPropertyChanged(nameof(NeedsDomain));
        _ = ValidateAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnPluginTypeChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPlugin));
        OnPropertyChanged(nameof(IsHttpProxyPlugin));
        OnPropertyChanged(nameof(IsSocks5Plugin));
        OnPropertyChanged(nameof(IsStaticFilePlugin));
        OnPropertyChanged(nameof(IsHttps2HTTPPlugin));
        OnPropertyChanged(nameof(IsHttp2HttpsPlugin));
    }

    public bool HasPlugin => !string.IsNullOrWhiteSpace(PluginType);
    public bool IsHttpProxyPlugin => PluginType == "http_proxy";
    public bool IsSocks5Plugin => PluginType == "socks5";
    public bool IsStaticFilePlugin => PluginType == "static_file";
    public bool IsHttps2HTTPPlugin => PluginType == "https2http";
    public bool IsHttp2HttpsPlugin => PluginType == "http2https";

    private void LoadFromProxy(ProxyConfig proxy)
    {
        ProxyName = proxy.Name;
        ProxyType = proxy.Type;
        LocalIP = proxy.LocalIP;
        LocalPort = proxy.LocalPort;
        RemotePort = proxy.RemotePort;
        CustomDomains = proxy.CustomDomains != null ? string.Join(", ", proxy.CustomDomains) : null;
        Subdomain = proxy.Subdomain;
        Locations = proxy.Locations;
        HttpUser = proxy.HttpUser;
        HttpPassword = proxy.HttpPassword;
        HostHeaderRewrite = proxy.HostHeaderRewrite;
        SecretKey = proxy.SecretKey;
        AllowUsers = proxy.AllowUsers;
        Multiplexer = proxy.Multiplexer;
        UseEncryption = proxy.Transport?.UseEncryption ?? false;
        UseCompression = proxy.Transport?.UseCompression ?? false;
        BandwidthLimit = proxy.Transport?.BandwidthLimit;
        BandwidthLimitMode = proxy.Transport?.BandwidthLimitMode;

        if (proxy.HealthCheck != null)
        {
            HealthCheckEnabled = true;
            HealthCheckType = proxy.HealthCheck.Type;
            HealthCheckTimeoutSeconds = proxy.HealthCheck.TimeoutSeconds;
            HealthCheckMaxFailed = proxy.HealthCheck.MaxFailed;
            HealthCheckIntervalSeconds = proxy.HealthCheck.IntervalSeconds;
            HealthCheckPath = proxy.HealthCheck.Path;
            if (proxy.HealthCheck.HttpHeaders != null)
            {
                HealthCheckHeaders = string.Join("\n", proxy.HealthCheck.HttpHeaders.Select(h => $"{h.Name}: {h.Value}"));
            }
        }

        if (proxy.Plugin != null)
        {
            PluginType = proxy.Plugin.Type;
            PluginHttpProxyUrl = proxy.Plugin.HttpProxyUrl;
            PluginSocks5Url = proxy.Plugin.Socks5Url;
            PluginStaticFilePath = proxy.Plugin.StaticFileLocalPath;
            PluginStaticFilePrefixUrl = proxy.Plugin.StaticFilePrefixUrl;
            PluginHttps2HttpLocalAddr = proxy.Plugin.Https2HttpLocalAddr;
            PluginHttps2HttpCrtPath = proxy.Plugin.Https2HttpCrtPath;
            PluginHttps2HttpKeyPath = proxy.Plugin.Https2HttpKeyPath;
            PluginHttp2HttpsLocalAddr = proxy.Plugin.Http2HttpsLocalAddr;
            PluginHttp2HttpsCrtPath = proxy.Plugin.Http2HttpsCrtPath;
            PluginHttp2HttpsKeyPath = proxy.Plugin.Http2HttpsKeyPath;
        }
    }

    private Task ValidateAsync()
    {
        var proxy = CreateProxyConfig();
        var validation = _validationService.ValidateProxy(proxy);
        IsValid = validation.IsValid;
        ValidationError = validation.Errors.FirstOrDefault();
        return Task.CompletedTask;
    }

    private ProxyConfig CreateProxyConfig()
    {
        ClientPluginOptions? plugin = null;
        if (!string.IsNullOrWhiteSpace(PluginType))
        {
            plugin = new ClientPluginOptions
            {
                Type = PluginType,
                HttpProxyUrl = PluginHttpProxyUrl,
                Socks5Url = PluginSocks5Url,
                StaticFileLocalPath = PluginStaticFilePath,
                StaticFilePrefixUrl = PluginStaticFilePrefixUrl,
                Https2HttpLocalAddr = PluginHttps2HttpLocalAddr,
                Https2HttpCrtPath = PluginHttps2HttpCrtPath,
                Https2HttpKeyPath = PluginHttps2HttpKeyPath,
                Http2HttpsLocalAddr = PluginHttp2HttpsLocalAddr,
                Http2HttpsCrtPath = PluginHttp2HttpsCrtPath,
                Http2HttpsKeyPath = PluginHttp2HttpsKeyPath
            };
        }

        List<HttpHeader>? httpHeaders = null;
        if (!string.IsNullOrWhiteSpace(HealthCheckHeaders))
        {
            httpHeaders = HealthCheckHeaders.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var parts = line.Split(':', 2);
                    return parts.Length == 2 ? new HttpHeader { Name = parts[0].Trim(), Value = parts[1].Trim() } : null;
                })
                .Where(h => h != null)
                .ToList()!;
        }

        return new ProxyConfig
        {
            Name = ProxyName,
            Type = ProxyType,
            LocalIP = LocalIP,
            LocalPort = LocalPort,
            RemotePort = RemotePort,
            CustomDomains = ParseList(CustomDomains),
            Subdomain = Subdomain,
            Locations = Locations,
            HttpUser = HttpUser,
            HttpPassword = HttpPassword,
            HostHeaderRewrite = HostHeaderRewrite,
            SecretKey = SecretKey,
            AllowUsers = AllowUsers,
            Multiplexer = Multiplexer,
            Transport = new ProxyTransport
            {
                UseEncryption = UseEncryption,
                UseCompression = UseCompression,
                BandwidthLimit = BandwidthLimit,
                BandwidthLimitMode = BandwidthLimitMode
            },
            HealthCheck = HealthCheckEnabled ? new HealthCheckConfig
            {
                Type = HealthCheckType,
                TimeoutSeconds = HealthCheckTimeoutSeconds,
                MaxFailed = HealthCheckMaxFailed,
                IntervalSeconds = HealthCheckIntervalSeconds,
                Path = HealthCheckPath,
                HttpHeaders = httpHeaders
            } : null,
            Plugin = plugin
        };
    }

    private static List<string>? ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;
            ValidationError = null;

            await ValidateAsync();

            if (!IsValid)
            {
                return;
            }

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                var proxy = CreateProxyConfig();

                // Remove original proxy if editing
                if (_originalProxy != null)
                {
                    config.Proxies.RemoveAll(p => p.Name == _originalProxy.Name);
                }

                config.Proxies.Add(proxy);

                await _configurationService.SaveConfigurationAsync(configPath, config);

                _logger.LogInformation("Proxy saved: {ProxyName}", proxy.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving proxy");
            ValidationError = "Failed to save proxy";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void AddLocation()
    {
        Locations ??= new List<string>();
        Locations.Add("/");
        OnPropertyChanged(nameof(Locations));
    }

    private void AddAllowUser()
    {
        AllowUsers ??= new List<string>();
        AllowUsers.Add("");
        OnPropertyChanged(nameof(AllowUsers));
    }
}
