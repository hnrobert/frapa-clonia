namespace FrapaClonia.Domain.Models;

/// <summary>
/// Complete frp client configuration matching frpc.toml schema
/// </summary>
public class FrpClientConfig
{
    /// <summary>
    /// Client common configuration (server connection, auth, etc.)
    /// </summary>
    public ClientCommonConfig? CommonConfig { get; set; }

    /// <summary>
    /// Proxy configurations
    /// </summary>
    public List<ProxyConfig> Proxies { get; set; } = new();

    /// <summary>
    /// Visitor configurations (for STCP/XTCP/SUDP)
    /// </summary>
    public List<VisitorConfig> Visitors { get; set; } = new();
}

/// <summary>
/// Client common configuration
/// </summary>
public class ClientCommonConfig
{
    // Authentication
    public AuthConfig? Auth { get; set; }
    public string? User { get; set; }

    // Server connection
    public string? ServerAddr { get; set; }
    public int ServerPort { get; set; } = 7000;
    public string? NatHoleStunServer { get; set; }

    // DNS
    public string? DnsServer { get; set; }

    // Behavior
    public bool LoginFailExit { get; set; } = true;
    public List<string>? Start { get; set; }

    // Logging
    public LogConfig? Log { get; set; }

    // Web server (admin UI)
    public WebServerConfig? WebServer { get; set; }

    // Transport
    public ClientTransportConfig? Transport { get; set; }

    // Virtual network (alpha feature)
    public VirtualNetConfig? VirtualNet { get; set; }

    // Feature gates
    public Dictionary<string, bool>? FeatureGates { get; set; }

    // UDP settings
    public int UdpPacketSize { get; set; } = 1500;

    // Metadata
    public Dictionary<string, string>? Metadata { get; set; }

    // Include additional config files
    public List<string>? Includes { get; set; }
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthConfig
{
    public string Method { get; set; } = "token";  // token or oidc
    public List<string>? AdditionalScopes { get; set; }
    public string? Token { get; set; }
    public ValueSource? TokenSource { get; set; }
    public AuthOIDCClientConfig? Oidc { get; set; }
}

/// <summary>
/// Value source (for loading from file)
/// </summary>
public class ValueSource
{
    public required string FileName { get; init; }
}

/// <summary>
/// OIDC authentication configuration
/// </summary>
public class AuthOIDCClientConfig
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Audience { get; init; }
    public string? Scope { get; init; }
    public required string TokenEndpointUrl { get; init; }
    public Dictionary<string, string>? AdditionalEndpointParams { get; init; }
    public string? TrustedCaFile { get; init; }
    public bool InsecureSkipVerify { get; init; }
    public string? ProxyUrl { get; init; }
}

/// <summary>
/// Log configuration
/// </summary>
public class LogConfig
{
    public string Level { get; set; } = "info";
    public string? To { get; set; }
    public int MaxDays { get; set; } = 3;
    public bool DisablePrintColor { get; set; }
}

/// <summary>
/// Web server configuration (admin UI)
/// </summary>
public class WebServerConfig
{
    public required string Addr { get; init; }
    public int Port { get; init; }
    public string? User { get; init; }
    public string? Password { get; init; }
    public string? Token { get; init; }
    public bool PprofEnable { get; init; }
}

/// <summary>
/// Client transport configuration
/// </summary>
public class ClientTransportConfig
{
    public string Protocol { get; set; } = "tcp";  // tcp, kcp, quic, websocket, wss
    public int DialServerTimeout { get; set; } = 10;
    public int? DialServerKeepalive { get; set; }
    public string? ConnectServerLocalIP { get; set; }
    public string? ProxyUrl { get; set; }
    public int? PoolCount { get; set; }
    public bool TcpMux { get; set; } = true;
    public int? TcpMuxKeepaliveInterval { get; set; }
    public QUICOptions? Quic { get; set; }
    public int HeartbeatInterval { get; set; } = 30;
    public int HeartbeatTimeout { get; set; } = 90;
    public TLSClientConfig? Tls { get; set; }
    public bool UseEncryption { get; set; }
    public bool UseCompression { get; set; }
}

/// <summary>
/// QUIC protocol options
/// </summary>
public class QUICOptions
{
    public int? KeepaliveInterval { get; init; }
    public int? MaxIdleTimeout { get; init; }
    public int? MaxIncomingStreams { get; init; }
}

/// <summary>
/// TLS client configuration
/// </summary>
public class TLSClientConfig
{
    public bool Enable { get; set; } = true;
    public bool DisableCustomTLSFirstByte { get; set; }
    public string? CertFile { get; set; }
    public string? KeyFile { get; set; }
    public string? CaFile { get; set; }
    public string? ServerName { get; set; }
}

/// <summary>
/// Virtual network configuration
/// </summary>
public class VirtualNetConfig
{
    public required string Address { get; init; }  // CIDR format, e.g., "100.86.0.1/24"
    public int? Mtu { get; init; }
    public List<string>? Routes { get; init; }
}
