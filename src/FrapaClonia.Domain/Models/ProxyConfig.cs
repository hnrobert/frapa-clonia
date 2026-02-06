namespace FrapaClonia.Domain.Models;

/// <summary>
/// Base proxy configuration
/// </summary>
public class ProxyConfig
{
    public required string Name { get; init; }
    public required string Type { get; init; }  // tcp, udp, http, https, tcpmux, stcp, sudp, xtcp
    public Dictionary<string, string>? Annotations { get; init; }
    public ProxyTransport? Transport { get; init; }
    public Dictionary<string, string>? Metadatas { get; init; }
    public LoadBalancerConfig? LoadBalancer { get; init; }
    public HealthCheckConfig? HealthCheck { get; init; }
    public ProxyBackend? Backend { get; init; }

    // Type-specific properties
    public int? RemotePort { get; init; }  // TCP, UDP
    public List<string>? CustomDomains { get; init; }  // HTTP, HTTPS, TCPMUX
    public string? Subdomain { get; init; }  // HTTP, HTTPS, TCPMUX
    public List<string>? Locations { get; init; }  // HTTP
    public string? HttpUser { get; init; }  // HTTP, TCPMUX
    public string? HttpPassword { get; init; }  // HTTP, TCPMUX
    public string? HostHeaderRewrite { get; init; }  // HTTP
    public HeaderOperations? RequestHeaders { get; init; }  // HTTP
    public HeaderOperations? ResponseHeaders { get; init; }  // HTTP
    public string? RouteByHTTPUser { get; init; }  // HTTP, TCPMUX
    public string? SecretKey { get; init; }  // STCP, XTCP, SUDP
    public List<string>? AllowUsers { get; init; }  // STCP, XTCP, SUDP
    public NatTraversalConfig? NatTraversal { get; init; }  // XTCP
    public string? Multiplexer { get; init; }  // TCPMUX
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
    public List<HTTPHeader>? HttpHeaders { get; init; }
}

/// <summary>
/// HTTP header
/// </summary>
public class HTTPHeader
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
    public string? Https2httpLocalAddr { get; init; }
    public string? Https2httpCrtPath { get; init; }
    public string? Https2httpKeyPath { get; init; }

    // HTTP to HTTPS
    public string? Http2httpsLocalAddr { get; init; }
    public string? Http2httpsCrtPath { get; init; }
    public string? Http2httpsKeyPath { get; init; }
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
    public required string Name { get; init; }
    public required string Type { get; init; }  // stcp, xtcp, sudp
    public required string ServerName { get; init; }
    public required string SecretKey { get; init; }
    public required string BindAddr { get; init; }
    public required int BindPort { get; init; }
    public string? BindIp { get; init; }
    public ClientTransportConfig? Transport { get; init; }
    public Dictionary<string, string>? Metadatas { get; init; }
}
