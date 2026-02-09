namespace FrapaClonia.Domain.Models;

/// <summary>
/// Base proxy configuration
/// </summary>
public class ProxyConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "tcp";  // tcp, udp, http, https, tcpmux, stcp, sudp, xtcp
    public Dictionary<string, string>? Annotations { get; set; }
    public ProxyTransport? Transport { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public LoadBalancerConfig? LoadBalancer { get; set; }
    public HealthCheckConfig? HealthCheck { get; set; }

    // Backend (local service) configuration - flat for TOML compatibility
    public string LocalIP { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; }
    public ClientPluginOptions? Plugin { get; set; }

    // Computed backend property for code convenience
    public ProxyBackend Backend => new()
    {
        LocalIP = LocalIP,
        LocalPort = LocalPort,
        Plugin = Plugin
    };

    // Type-specific properties
    public int? RemotePort { get; set; }  // TCP, UDP
    public List<string>? CustomDomains { get; set; }  // HTTP, HTTPS, TCPMUX
    public string? Subdomain { get; set; }  // HTTP, HTTPS, TCPMUX
    public List<string>? Locations { get; set; }  // HTTP
    public string? HttpUser { get; set; }  // HTTP, TCPMUX
    public string? HttpPassword { get; set; }  // HTTP, TCPMUX
    public string? HostHeaderRewrite { get; set; }  // HTTP
    public HeaderOperations? RequestHeaders { get; set; }  // HTTP
    public HeaderOperations? ResponseHeaders { get; set; }  // HTTP
    public string? RouteByHttpUser { get; set; }  // HTTP, TCPMUX
    public string? SecretKey { get; set; }  // STCP, XTCP, SUDP
    public List<string>? AllowUsers { get; set; }  // STCP, XTCP, SUDP
    public NatTraversalConfig? NatTraversal { get; set; }  // XTCP
    public string? Multiplexer { get; set; }  // TCPMUX
}

/// <summary>
/// Proxy transport configuration
/// </summary>
public class ProxyTransport
{
    public bool UseEncryption { get; init; }
    public bool UseCompression { get; init; }
    public string? BandwidthLimit { get; init; }  // e.g., "1MB", "256KB"
    public string? BandwidthLimitMode { get; init; }  // client or server
    public string? ProxyProtocolVersion { get; init; }  // v1 or v2
}

/// <summary>
/// Proxy backend configuration
/// </summary>
public class ProxyBackend
{
    public string LocalIP { get; init; } = "127.0.0.1";
    public required int LocalPort { get; init; }
    public ClientPluginOptions? Plugin { get; init; }
}

/// <summary>
/// Load balancer configuration
/// </summary>
public class LoadBalancerConfig
{
    public required string Group { get; init; }
    public string? GroupKey { get; init; }
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfig
{
    public required string Type { get; init; }  // tcp or http
    public int TimeoutSeconds { get; init; } = 3;
    public int MaxFailed { get; init; } = 1;
    public int IntervalSeconds { get; init; } = 10;
    public string? Path { get; init; }  // for http type
    public List<HttpHeader>? HttpHeaders { get; init; }
}

/// <summary>
/// HTTP header
/// </summary>
public class HttpHeader
{
    public required string Name { get; init; }
    public required string Value { get; init; }
}

/// <summary>
/// Header operations
/// </summary>
public class HeaderOperations
{
    public bool? Set { get; init; }
    public Dictionary<string, string>? Add { get; init; }
    public List<string>? Remove { get; init; }
}

/// <summary>
/// Client plugin options
/// </summary>
public class ClientPluginOptions
{
    public required string Type { get; init; }  // http_proxy, socks5, static_file, https2http, http2https

    // HTTP/SOCKS proxy
    public string? HttpProxyUrl { get; init; }
    public string? Socks5Url { get; init; }

    // Static file
    public string? StaticFileLocalPath { get; init; }
    public string? StaticFilePrefixUrl { get; init; }

    // HTTPS to HTTP
    public string? Https2HttpLocalAddr { get; init; }
    public string? Https2HttpCrtPath { get; init; }
    public string? Https2HttpKeyPath { get; init; }

    // HTTP to HTTPS
    public string? Http2HttpsLocalAddr { get; init; }
    public string? Http2HttpsCrtPath { get; init; }
    public string? Http2HttpsKeyPath { get; init; }
}

/// <summary>
/// NAT traversal configuration
/// </summary>
public class NatTraversalConfig
{
    public string? Role { get; init; }
    public int? KeepaliveInterval { get; init; }
}

/// <summary>
/// Visitor configuration
/// </summary>
public class VisitorConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "stcp";  // stcp, xtcp, sudp
    public string ServerName { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BindAddr { get; set; } = "127.0.0.1";
    public int BindPort { get; set; }
    public string? BindIp { get; set; }
    public ClientTransportConfig? Transport { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
