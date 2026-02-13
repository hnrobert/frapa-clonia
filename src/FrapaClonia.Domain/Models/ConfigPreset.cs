using CommunityToolkit.Mvvm.ComponentModel;

namespace FrapaClonia.Domain.Models;

/// <summary>
/// Represents a configuration preset containing frpc configuration and deployment settings
/// </summary>
public partial class ConfigPreset : ObservableObject
{
    /// <summary>
    /// Unique identifier for this preset
    /// </summary>
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    /// <summary>
    /// Display name for this preset
    /// </summary>
    [ObservableProperty]
    private string _name = "Default";

    /// <summary>
    /// When this preset was created
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// When this preset was last modified
    /// </summary>
    [ObservableProperty]
    private DateTime _modifiedAt = DateTime.Now;

    /// <summary>
    /// The frpc configuration (server, proxies, visitors)
    /// </summary>
    [ObservableProperty]
    private FrpClientConfig _configuration = new();

    /// <summary>
    /// Deployment settings for this preset
    /// </summary>
    [ObservableProperty]
    private DeploymentSettings _deployment = new();

    /// <summary>
    /// Creates a new ConfigPreset with default values
    /// </summary>
    public ConfigPreset() { }

    /// <summary>
    /// Creates a new ConfigPreset with specified name
    /// </summary>
    public ConfigPreset(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a deep copy of this preset
    /// </summary>
    public ConfigPreset Clone()
    {
        return new ConfigPreset
        {
            Id = Guid.NewGuid(),
            Name = $"{Name} (Copy)",
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now,
            Configuration = CloneConfiguration(Configuration),
            Deployment = new DeploymentSettings
            {
                DeploymentMode = Deployment.DeploymentMode,
                DockerContainerName = Deployment.DockerContainerName,
                DockerImageName = Deployment.DockerImageName,
                DockerImageTag = Deployment.DockerImageTag,
                FrpcBinaryPath = Deployment.FrpcBinaryPath,
                FrpcVersion = Deployment.FrpcVersion,
                InstallMethod = Deployment.InstallMethod,
                SelectedPackageManager = Deployment.SelectedPackageManager,
                ServiceScope = Deployment.ServiceScope,
                AutoStartOnBoot = Deployment.AutoStartOnBoot,
                ServiceEnabled = Deployment.ServiceEnabled
            }
        };
    }

    private static FrpClientConfig CloneConfiguration(FrpClientConfig source)
    {
        return new FrpClientConfig
        {
            CommonConfig = source.CommonConfig != null ? new ClientCommonConfig
            {
                Auth = source.CommonConfig.Auth != null ? new AuthConfig
                {
                    Method = source.CommonConfig.Auth.Method,
                    AdditionalScopes = source.CommonConfig.Auth.AdditionalScopes?.ToList(),
                    Token = source.CommonConfig.Auth.Token,
                    Oidc = source.CommonConfig.Auth.Oidc != null ? new AuthOIDCClientConfig
                    {
                        ClientId = source.CommonConfig.Auth.Oidc.ClientId,
                        ClientSecret = source.CommonConfig.Auth.Oidc.ClientSecret,
                        Audience = source.CommonConfig.Auth.Oidc.Audience,
                        Scope = source.CommonConfig.Auth.Oidc.Scope,
                        TokenEndpointUrl = source.CommonConfig.Auth.Oidc.TokenEndpointUrl,
                        AdditionalEndpointParams = source.CommonConfig.Auth.Oidc.AdditionalEndpointParams?.ToDictionary(kv => kv.Key, kv => kv.Value),
                        TrustedCaFile = source.CommonConfig.Auth.Oidc.TrustedCaFile,
                        InsecureSkipVerify = source.CommonConfig.Auth.Oidc.InsecureSkipVerify,
                        ProxyUrl = source.CommonConfig.Auth.Oidc.ProxyUrl
                    } : null,
                    TokenSource = source.CommonConfig.Auth.TokenSource
                } : null,
                User = source.CommonConfig.User,
                ServerAddr = source.CommonConfig.ServerAddr,
                ServerPort = source.CommonConfig.ServerPort,
                NatHoleStunServer = source.CommonConfig.NatHoleStunServer,
                DnsServer = source.CommonConfig.DnsServer,
                LoginFailExit = source.CommonConfig.LoginFailExit,
                Start = source.CommonConfig.Start?.ToList(),
                Log = source.CommonConfig.Log != null ? new LogConfig
                {
                    Level = source.CommonConfig.Log.Level,
                    To = source.CommonConfig.Log.To,
                    MaxDays = source.CommonConfig.Log.MaxDays,
                    DisablePrintColor = source.CommonConfig.Log.DisablePrintColor
                } : null,
                WebServer = source.CommonConfig.WebServer,
                Transport = source.CommonConfig.Transport != null ? new ClientTransportConfig
                {
                    Protocol = source.CommonConfig.Transport.Protocol,
                    DialServerTimeout = source.CommonConfig.Transport.DialServerTimeout,
                    DialServerKeepalive = source.CommonConfig.Transport.DialServerKeepalive,
                    ConnectServerLocalIP = source.CommonConfig.Transport.ConnectServerLocalIP,
                    ProxyUrl = source.CommonConfig.Transport.ProxyUrl,
                    PoolCount = source.CommonConfig.Transport.PoolCount,
                    TcpMux = source.CommonConfig.Transport.TcpMux,
                    TcpMuxKeepaliveInterval = source.CommonConfig.Transport.TcpMuxKeepaliveInterval,
                    Quic = source.CommonConfig.Transport.Quic,
                    HeartbeatInterval = source.CommonConfig.Transport.HeartbeatInterval,
                    HeartbeatTimeout = source.CommonConfig.Transport.HeartbeatTimeout,
                    Tls = source.CommonConfig.Transport.Tls != null ? new TLSClientConfig
                    {
                        Enable = source.CommonConfig.Transport.Tls.Enable,
                        DisableCustomTLSFirstByte = source.CommonConfig.Transport.Tls.DisableCustomTLSFirstByte,
                        CertFile = source.CommonConfig.Transport.Tls.CertFile,
                        KeyFile = source.CommonConfig.Transport.Tls.KeyFile,
                        CaFile = source.CommonConfig.Transport.Tls.CaFile,
                        ServerName = source.CommonConfig.Transport.Tls.ServerName
                    } : null,
                    UseEncryption = source.CommonConfig.Transport.UseEncryption,
                    UseCompression = source.CommonConfig.Transport.UseCompression
                } : null,
                VirtualNet = source.CommonConfig.VirtualNet,
                FeatureGates = source.CommonConfig.FeatureGates?.ToDictionary(kv => kv.Key, kv => kv.Value),
                UdpPacketSize = source.CommonConfig.UdpPacketSize,
                Metadata = source.CommonConfig.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
                Includes = source.CommonConfig.Includes?.ToList()
            } : null,
            Proxies = source.Proxies.Select(p => new ProxyConfig
            {
                Name = p.Name,
                Type = p.Type,
                Annotations = p.Annotations?.ToDictionary(kv => kv.Key, kv => kv.Value),
                Transport = p.Transport,
                Metadata = p.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
                LoadBalancer = p.LoadBalancer,
                HealthCheck = p.HealthCheck,
                LocalIP = p.LocalIP,
                LocalPort = p.LocalPort,
                Plugin = p.Plugin,
                RemotePort = p.RemotePort,
                CustomDomains = p.CustomDomains?.ToList(),
                Subdomain = p.Subdomain,
                Locations = p.Locations?.ToList(),
                HttpUser = p.HttpUser,
                HttpPassword = p.HttpPassword,
                HostHeaderRewrite = p.HostHeaderRewrite,
                RequestHeaders = p.RequestHeaders,
                ResponseHeaders = p.ResponseHeaders,
                RouteByHttpUser = p.RouteByHttpUser,
                SecretKey = p.SecretKey,
                AllowUsers = p.AllowUsers?.ToList(),
                NatTraversal = p.NatTraversal,
                Multiplexer = p.Multiplexer
            }).ToList(),
            Visitors = source.Visitors.Select(v => new VisitorConfig
            {
                Name = v.Name,
                Type = v.Type,
                ServerName = v.ServerName,
                SecretKey = v.SecretKey,
                BindAddr = v.BindAddr,
                BindPort = v.BindPort,
                BindIp = v.BindIp,
                Transport = v.Transport,
                Metadata = v.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value)
            }).ToList()
        };
    }
}

/// <summary>
/// Deployment settings for a configuration preset
/// </summary>
public partial class DeploymentSettings : ObservableObject
{
    /// <summary>
    /// Deployment mode: "native" or "docker"
    /// </summary>
    [ObservableProperty]
    private string _deploymentMode = "native";

    #region Docker Settings

    /// <summary>
    /// Docker container name
    /// </summary>
    [ObservableProperty]
    private string _dockerContainerName = "frapa-clonia-frpc";

    /// <summary>
    /// Docker image name
    /// </summary>
    [ObservableProperty]
    private string _dockerImageName = "fatedier/frpc:latest";

    /// <summary>
    /// Docker image tag
    /// </summary>
    [ObservableProperty]
    private string _dockerImageTag = "latest";

    #endregion

    #region Native Settings

    /// <summary>
    /// Custom frpc binary path if selected
    /// </summary>
    [ObservableProperty]
    private string? _frpcBinaryPath;

    /// <summary>
    /// Selected frpc version (e.g., "0.62.1" or "latest")
    /// </summary>
    [ObservableProperty]
    private string? _frpcVersion = "latest";

    /// <summary>
    /// Installation method: "auto", "github", "package_manager", "custom_path"
    /// </summary>
    [ObservableProperty]
    private string _installMethod = "auto";

    #endregion

    #region Package Manager Settings

    /// <summary>
    /// Selected package manager (e.g., "brew", "scoop", "choco", "apt", "pacman")
    /// </summary>
    [ObservableProperty]
    private string? _selectedPackageManager;

    #endregion

    #region Service Settings

    /// <summary>
    /// Service scope: "user" or "system"
    /// </summary>
    [ObservableProperty]
    private string _serviceScope = "user";

    /// <summary>
    /// Whether to start the service on boot
    /// </summary>
    [ObservableProperty]
    private bool _autoStartOnBoot = true;

    /// <summary>
    /// Whether the service is enabled
    /// </summary>
    [ObservableProperty]
    private bool _serviceEnabled = true;

    #endregion

    /// <summary>
    /// Creates a new DeploymentSettings with default values
    /// </summary>
    public DeploymentSettings() { }
}

/// <summary>
/// Wrapper class for preset items in the dropdown (includes the "+ New Preset..." option)
/// </summary>
public class PresetItem
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = "";
    public bool IsNewPresetOption { get; init; }

    public PresetItem(ConfigPreset preset)
    {
        Id = preset.Id;
        Name = preset.Name;
        IsNewPresetOption = false;
    }

    public PresetItem(string name, bool isNewPresetOption)
    {
        Name = name;
        IsNewPresetOption = isNewPresetOption;
    }

    public override string ToString() => Name;
}
