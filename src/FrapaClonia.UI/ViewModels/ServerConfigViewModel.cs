using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedParameterInPartialMethod

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for server connection configuration
/// </summary>
public partial class ServerConfigViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;
    private readonly ILogger<ServerConfigViewModel> _logger;

    [ObservableProperty]
    private string _serverAddr = "";

    [ObservableProperty]
    private int _serverPort = 7000;

    [ObservableProperty]
    private string _user = "";

    [ObservableProperty]
    private string _token = "";

    [ObservableProperty]
    private string _authMethod = "token"; // token or oidc

    // OIDC properties
    [ObservableProperty]
    private string? _oidcClientId;

    [ObservableProperty]
    private string? _oidcClientSecret;

    [ObservableProperty]
    private string? _oidcAudience;

    [ObservableProperty]
    private string? _oidcScope;

    [ObservableProperty]
    private string? _oidcTokenEndpointUrl;

    // Transport properties
    [ObservableProperty]
    private string _transportProtocol = "tcp"; // tcp, kcp, quic, websocket, wss

    [ObservableProperty]
    private int _dialServerTimeout = 10;

    [ObservableProperty]
    private bool _tcpMux = true;

    [ObservableProperty]
    private int _heartbeatInterval = 30;

    [ObservableProperty]
    private int _heartbeatTimeout = 90;

    [ObservableProperty]
    private bool _tlsEnabled = true;

    // DNS
    [ObservableProperty]
    private string? _dnsServer;

    // Log configuration
    [ObservableProperty]
    private string _logLevel = "info";

    [ObservableProperty]
    private string? _logTo;

    [ObservableProperty]
    private int _logMaxDays = 3;

    // Validation
    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private string? _validationWarning;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isLoading;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetCommand { get; }

    // ComboBox index properties for binding
    public int AuthMethodIndex
    {
        get => AuthMethod == "oidc" ? 1 : 0;
        set => AuthMethod = value == 1 ? "oidc" : "token";
    }

    public int TransportProtocolIndex
    {
        get => TransportProtocol switch
        {
            "kcp" => 1,
            "quic" => 2,
            "websocket" => 3,
            "wss" => 4,
            _ => 0
        };
        set => TransportProtocol = value switch
        {
            1 => "kcp",
            2 => "quic",
            3 => "websocket",
            4 => "wss",
            _ => "tcp"
        };
    }

    public int LogLevelIndex
    {
        get => LogLevel switch
        {
            "trace" => 0,
            "debug" => 1,
            "info" => 2,
            "warn" => 3,
            "error" => 4,
            _ => 2
        };
        set => LogLevel = value switch
        {
            0 => "trace",
            1 => "debug",
            2 => "info",
            3 => "warn",
            4 => "error",
            _ => "info"
        };
    }

    public ServerConfigViewModel(
        IConfigurationService configurationService,
        IValidationService validationService,
        ILogger<ServerConfigViewModel> logger)
    {
        _configurationService = configurationService;
        _validationService = validationService;
        _logger = logger;

        SaveCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveConfigurationAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not save configuration");
            }
        }, () => !IsSaving);
        ResetCommand = new RelayCommand(async void () =>
        {
            try
            {
                await LoadConfigurationAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not load configuration");
            }
        }, () => !IsSaving && !IsLoading);

        // Load saved configuration if exists
        _ = Task.Run(LoadConfigurationAsync);
    }

    partial void OnServerAddrChanged(string value)
    {
        _ = ValidateAsync();
    }

    partial void OnServerPortChanged(int value)
    {
        _ = ValidateAsync();
    }

    partial void OnTokenChanged(string value)
    {
        _ = ValidateAsync();
    }

    partial void OnAuthMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsOidcAuth));
        _ = ValidateAsync();
    }

    partial void OnValidationErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasValidationError));
    }

    partial void OnValidationWarningChanged(string? value)
    {
        OnPropertyChanged(nameof(HasValidationWarning));
    }

    public bool IsOidcAuth => AuthMethod == "oidc";
    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);
    public bool HasValidationWarning => !string.IsNullOrWhiteSpace(ValidationWarning);

    private async Task LoadConfigurationAsync()
    {
        try
        {
            IsLoading = true;

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config?.CommonConfig != null)
            {
                var cc = config.CommonConfig;

                // Load server settings
                ServerAddr = cc.ServerAddr ?? "";
                ServerPort = cc.ServerPort;
                User = cc.User ?? "";

                // Load auth
                if (cc.Auth != null)
                {
                    AuthMethod = cc.Auth.Method;
                    Token = cc.Auth.Token ?? "";
                    OidcClientId = cc.Auth.Oidc?.ClientId;
                    OidcClientSecret = cc.Auth.Oidc?.ClientSecret;
                    OidcAudience = cc.Auth.Oidc?.Audience;
                    OidcScope = cc.Auth.Oidc?.Scope;
                    OidcTokenEndpointUrl = cc.Auth.Oidc?.TokenEndpointUrl;
                }

                // Load transport
                if (cc.Transport != null)
                {
                    TransportProtocol = cc.Transport.Protocol;
                    DialServerTimeout = cc.Transport.DialServerTimeout;
                    TcpMux = cc.Transport.TcpMux;
                    HeartbeatInterval = cc.Transport.HeartbeatInterval;
                    HeartbeatTimeout = cc.Transport.HeartbeatTimeout;
                    TlsEnabled = cc.Transport.Tls?.Enable ?? true;
                }

                // Load DNS
                DnsServer = cc.DnsServer;

                // Load log
                if (cc.Log != null)
                {
                    LogLevel = cc.Log.Level;
                    LogTo = cc.Log.To;
                    LogMaxDays = cc.Log.MaxDays;
                }

                _logger.LogInformation("Configuration loaded successfully");
            }

            await ValidateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            ValidationError = "Failed to load configuration";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            IsSaving = true;
            ValidationError = null;

            // Create configuration
            var config = new FrpClientConfig
            {
                CommonConfig = new ClientCommonConfig
                {
                    ServerAddr = string.IsNullOrWhiteSpace(ServerAddr) ? null : ServerAddr,
                    ServerPort = ServerPort,
                    User = string.IsNullOrWhiteSpace(User) ? null : User,
                    Auth = CreateAuthConfig(),
                    Transport = CreateTransportConfig(),
                    DnsServer = DnsServer,
                    Log = CreateLogConfig()
                }
            };

            // Validate before saving
            var validation = _validationService.ValidateServerConnection(config.CommonConfig);
            if (!validation.IsValid)
            {
                ValidationError = validation.Errors.FirstOrDefault() ?? "Validation failed";
                return;
            }

            // Save configuration
            var configPath = _configurationService.GetDefaultConfigPath();
            await _configurationService.SaveConfigurationAsync(configPath, config);

            _logger.LogInformation("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            ValidationError = "Failed to save configuration: " + ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private Task ValidateAsync()
    {
        var config = new FrpClientConfig
        {
            CommonConfig = new ClientCommonConfig
            {
                ServerAddr = string.IsNullOrWhiteSpace(ServerAddr) ? null : ServerAddr,
                ServerPort = ServerPort,
                User = string.IsNullOrWhiteSpace(User) ? null : User,
                Auth = CreateAuthConfig(),
                Transport = CreateTransportConfig()
            }
        };

        var validation = _validationService.ValidateServerConnection(config.CommonConfig);
        IsValid = validation.IsValid;
        ValidationError = validation.Errors.FirstOrDefault();
        ValidationWarning = validation.Warnings.FirstOrDefault();
        return Task.CompletedTask;
    }

    private AuthConfig? CreateAuthConfig()
    {
        if (AuthMethod == "oidc")
        {
            return new AuthConfig
            {
                Method = "oidc",
                Oidc = new AuthOIDCClientConfig
                {
                    ClientId = OidcClientId ?? "",
                    ClientSecret = OidcClientSecret ?? "",
                    Audience = OidcAudience,
                    Scope = OidcScope,
                    TokenEndpointUrl = OidcTokenEndpointUrl ?? ""
                }
            };
        }

        return string.IsNullOrWhiteSpace(Token) ? null : new AuthConfig
        {
            Method = "token",
            Token = Token
        };
    }

    private ClientTransportConfig CreateTransportConfig()
    {
        return new ClientTransportConfig
        {
            Protocol = TransportProtocol,
            DialServerTimeout = DialServerTimeout,
            TcpMux = TcpMux,
            HeartbeatInterval = HeartbeatInterval,
            HeartbeatTimeout = HeartbeatTimeout,
            Tls = new TLSClientConfig
            {
                Enable = TlsEnabled
            }
        };
    }

    private LogConfig CreateLogConfig()
    {
        return new LogConfig
        {
            Level = LogLevel,
            To = LogTo,
            MaxDays = LogMaxDays
        };
    }
}
