using System.Reflection;
using System.Text;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Core.Services;

/// <summary>
/// Service for serializing/deserializing frpc.toml files
/// </summary>
public class TomlSerializer(ILogger<TomlSerializer> logger) : ITomlSerializer
{
    public Task<FrpClientConfig?> DeserializeAsync(string tomlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = ParseToml(tomlContent);
            return Task.FromResult<FrpClientConfig?>(config);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing TOML content");
            return Task.FromResult<FrpClientConfig?>(null);
        }
    }

    public Task<FrpClientConfig?> DeserializeFromFileAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogDebug("TOML file not found at {FilePath}, returning empty config", filePath);
                return Task.FromResult<FrpClientConfig?>(new FrpClientConfig
                {
                    CommonConfig = new ClientCommonConfig(),
                    Proxies = [],
                    Visitors = []
                });
            }

            var tomlContent = File.ReadAllText(filePath, Encoding.UTF8);
            logger.LogDebug("Deserializing TOML file at {FilePath}", filePath);
            return DeserializeAsync(tomlContent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading TOML file at {FilePath}", filePath);
            return Task.FromResult<FrpClientConfig?>(null);
        }
    }

    public Task<string> SerializeAsync(FrpClientConfig configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();

            if (configuration.CommonConfig != null)
            {
                var common = SerializeClientCommonConfig(configuration.CommonConfig);
                if (!string.IsNullOrWhiteSpace(common))
                {
                    sb.AppendLine(common);
                    sb.AppendLine();
                }
            }

            foreach (var proxy in configuration.Proxies)
            {
                sb.AppendLine("[[proxies]]");
                sb.AppendLine(SerializeProxy(proxy));
                sb.AppendLine();
            }

            foreach (var visitor in configuration.Visitors)
            {
                sb.AppendLine("[[visitors]]");
                sb.AppendLine(SerializeVisitor(visitor));
                sb.AppendLine();
            }

            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error serializing configuration to TOML");
            return Task.FromResult(string.Empty);
        }
    }

    public Task SerializeToFileAsync(string filePath, FrpClientConfig configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var content = SerializeAsync(configuration, cancellationToken).Result;
            File.WriteAllText(filePath, content);
            logger.LogDebug("Serialized configuration to file at {FilePath}", filePath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing TOML file at {FilePath}", filePath);
            return Task.CompletedTask;
        }
    }

    #region Parsing

    private static FrpClientConfig ParseToml(string tomlContent)
    {
        var config = new FrpClientConfig
        {
            CommonConfig = new ClientCommonConfig(),
            Proxies = [],
            Visitors = []
        };

        var section = "";
        var inArray = false;
        var arrayName = "";
        var arraySub = "";

        foreach (var line in tomlContent.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            // Array section header: [[proxies]] or [[visitors]]
            if (trimmed.StartsWith("[[") && trimmed.EndsWith("]]"))
            {
                arrayName = trimmed[2..^2].Trim();
                inArray = true;
                arraySub = "";
                section = "";

                switch (arrayName)
                {
                    case "proxies":
                        config.Proxies.Add(new ProxyConfig { Name = "", Type = "tcp", LocalIP = "127.0.0.1" });
                        break;
                    case "visitors":
                        config.Visitors.Add(new VisitorConfig { Name = "", Type = "stcp", BindAddr = "127.0.0.1" });
                        break;
                }

                continue;
            }

            // Regular section header
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var sectionName = trimmed[1..^1].Trim();
                var dotIndex = sectionName.IndexOf('.');

                if (dotIndex > 0)
                {
                    var prefix = sectionName[..dotIndex];
                    var subPath = sectionName[(dotIndex + 1)..];

                    if (prefix == "proxies" || prefix == "visitors")
                    {
                        inArray = true;
                        arrayName = prefix;
                        arraySub = subPath;
                        section = "";
                    }
                    else
                    {
                        inArray = false;
                        section = sectionName;
                    }
                }
                else
                {
                    inArray = false;
                    section = sectionName;
                }

                continue;
            }

            // Key-value pair
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0) continue;
            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim();

            if (value.StartsWith('"') && value.EndsWith('"'))
                value = value[1..^1];

            // Inline table
            if (value.StartsWith('{') && value.EndsWith('}'))
            {
                var dict = ParseInlineTable(value[1..^1]);
                SetConfigValue(config, section, inArray, arrayName, arraySub, key, dict);
                continue;
            }

            // Array
            if (value.StartsWith('[') && value.EndsWith(']'))
            {
                var inner = value[1..^1].Trim();
                if (inner.StartsWith('{'))
                {
                    var tables = ParseArrayOfInlineTables(inner);
                    SetConfigValue(config, section, inArray, arrayName, arraySub, key, tables);
                }
                else
                {
                    var list = inner
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(v => v.Trim('"')).ToList();
                    SetConfigValue(config, section, inArray, arrayName, arraySub, key, list);
                }

                continue;
            }

            // Scalar
            object parsedValue;
            if (bool.TryParse(value, out var boolVal))
                parsedValue = boolVal;
            else if (int.TryParse(value, out var intVal))
                parsedValue = intVal;
            else
                parsedValue = value;

            SetConfigValue(config, section, inArray, arrayName, arraySub, key, parsedValue);
        }

        return config;
    }

    private static Dictionary<string, string> ParseInlineTable(string content)
    {
        var dict = new Dictionary<string, string>();
        foreach (var pair in content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx <= 0) continue;
            var k = pair[..eqIdx].Trim().Trim('"');
            var v = pair[(eqIdx + 1)..].Trim().Trim('"');
            dict[k] = v;
        }

        return dict;
    }

    private static List<Dictionary<string, string>> ParseArrayOfInlineTables(string content)
    {
        var result = new List<Dictionary<string, string>>();
        var depth = 0;
        var start = -1;

        for (var i = 0; i < content.Length; i++)
        {
            switch (content[i])
            {
                case '{':
                {
                    if (depth == 0) start = i;
                    depth++;
                    break;
                }
                case '}':
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        result.Add(ParseInlineTable(content[(start + 1)..i]));
                        start = -1;
                    }

                    break;
                }
            }
        }

        return result;
    }

    private static void SetConfigValue(FrpClientConfig config, string section, bool inArray,
        string arrayName, string arraySub, string key, object value)
    {
        if (inArray)
        {
            switch (arrayName)
            {
                case "proxies" when config.Proxies.Count > 0:
                    SetProxyContextValue(config.Proxies[^1], arraySub, key, value);
                    break;
                case "visitors" when config.Visitors.Count > 0:
                    SetVisitorContextValue(config.Visitors[^1], arraySub, key, value);
                    break;
            }
        }
        else
        {
            SetCommonContextValue(config, section, key, value);
        }
    }

    #region Common Config Setters

    private static void SetCommonContextValue(FrpClientConfig config, string section, string key, object value)
    {
        var common = config.CommonConfig!;
        switch (section)
        {
            case "":
                SetCommonConfigValue(common, key, value);
                break;
            case "auth":
                common.Auth ??= new AuthConfig();
                SetAuthValue(common.Auth, key, value);
                break;
            case "auth.tokenSource":
                common.Auth ??= new AuthConfig();
                common.Auth.TokenSource ??= new ValueSource { FileName = "" };
                if (key == "fileName") SetProp(common.Auth.TokenSource, "FileName", value.ToString());
                break;
            case "auth.oidc":
                common.Auth ??= new AuthConfig();
                common.Auth.Oidc ??= new AuthOIDCClientConfig
                    { ClientId = "", ClientSecret = "", TokenEndpointUrl = "" };
                SetOidcValue(common.Auth.Oidc, key, value);
                break;
            case "transport":
                common.Transport ??= new ClientTransportConfig();
                SetClientTransportValue(common.Transport, key, value);
                break;
            case "transport.tls":
                common.Transport ??= new ClientTransportConfig();
                common.Transport.Tls ??= new TLSClientConfig();
                SetTlsValue(common.Transport.Tls, key, value);
                break;
            case "transport.quic":
                common.Transport ??= new ClientTransportConfig();
                common.Transport.Quic ??= new QUICOptions();
                SetQuicValue(common.Transport.Quic, key, value);
                break;
            case "log":
                common.Log ??= new LogConfig();
                SetLogValue(common.Log, key, value);
                break;
            case "webServer":
                common.WebServer ??= new WebServerConfig { Addr = "" };
                SetWebServerValue(common.WebServer, key, value);
                break;
            case "virtualNet":
                common.VirtualNet ??= new VirtualNetConfig { Address = "" };
                SetVirtualNetValue(common.VirtualNet, key, value);
                break;
            case "metadata":
                common.Metadata ??= new Dictionary<string, string>();
                if (value is string s) common.Metadata[key] = s;
                break;
            case "featureGates":
                common.FeatureGates ??= new Dictionary<string, bool>();
                if (value is bool b) common.FeatureGates[key] = b;
                break;
        }
    }

    private static void SetCommonConfigValue(ClientCommonConfig config, string key, object value)
    {
        switch (key)
        {
            case "user": config.User = value.ToString(); break;
            case "serverAddr": config.ServerAddr = value.ToString(); break;
            case "serverPort": config.ServerPort = Convert.ToInt32(value); break;
            case "natHoleStunServer": config.NatHoleStunServer = value.ToString(); break;
            case "dnsServer": config.DnsServer = value.ToString(); break;
            case "loginFailExit": config.LoginFailExit = Convert.ToBoolean(value); break;
            case "start": config.Start = (List<string>)value; break;
            case "udpPacketSize": config.UdpPacketSize = Convert.ToInt32(value); break;
            case "includes": config.Includes = (List<string>)value; break;
        }
    }

    private static void SetAuthValue(AuthConfig auth, string key, object value)
    {
        switch (key)
        {
            case "method": auth.Method = value.ToString() ?? "token"; break;
            case "token": auth.Token = value.ToString(); break;
            case "additionalScopes": auth.AdditionalScopes = (List<string>)value; break;
        }
    }

    private static void SetOidcValue(AuthOIDCClientConfig oidc, string key, object value)
    {
        switch (key)
        {
            case "clientId": SetProp(oidc, "ClientId", value.ToString()); break;
            case "clientSecret": SetProp(oidc, "ClientSecret", value.ToString()); break;
            case "audience": SetProp(oidc, "Audience", value.ToString()); break;
            case "scope": SetProp(oidc, "Scope", value.ToString()); break;
            case "tokenEndpointUrl": SetProp(oidc, "TokenEndpointUrl", value.ToString()); break;
            case "additionalEndpointParams" when value is Dictionary<string, string> dict:
                SetProp(oidc, "AdditionalEndpointParams", dict);
                break;
            case "trustedCaFile": SetProp(oidc, "TrustedCaFile", value.ToString()); break;
            case "insecureSkipVerify": SetProp(oidc, "InsecureSkipVerify", Convert.ToBoolean(value)); break;
            case "proxyUrl": SetProp(oidc, "ProxyUrl", value.ToString()); break;
        }
    }

    private static void SetClientTransportValue(ClientTransportConfig transport, string key,
        object value)
    {
        switch (key)
        {
            case "protocol": transport.Protocol = value.ToString() ?? "tcp"; break;
            case "dialServerTimeout": transport.DialServerTimeout = Convert.ToInt32(value); break;
            case "dialServerKeepalive": transport.DialServerKeepalive = Convert.ToInt32(value); break;
            case "connectServerLocalIP": transport.ConnectServerLocalIP = value.ToString(); break;
            case "proxyUrl": transport.ProxyUrl = value.ToString(); break;
            case "poolCount": transport.PoolCount = Convert.ToInt32(value); break;
            case "tcpMux": transport.TcpMux = Convert.ToBoolean(value); break;
            case "tcpMuxKeepaliveInterval": transport.TcpMuxKeepaliveInterval = Convert.ToInt32(value); break;
            case "heartbeatInterval": transport.HeartbeatInterval = Convert.ToInt32(value); break;
            case "heartbeatTimeout": transport.HeartbeatTimeout = Convert.ToInt32(value); break;
            case "useEncryption": transport.UseEncryption = Convert.ToBoolean(value); break;
            case "useCompression": transport.UseCompression = Convert.ToBoolean(value); break;
        }
    }

    private static void SetTlsValue(TLSClientConfig tls, string key, object value)
    {
        switch (key)
        {
            case "enable": tls.Enable = Convert.ToBoolean(value); break;
            case "disableCustomTLSFirstByte": tls.DisableCustomTLSFirstByte = Convert.ToBoolean(value); break;
            case "certFile": tls.CertFile = value.ToString(); break;
            case "keyFile": tls.KeyFile = value.ToString(); break;
            case "caFile": tls.CaFile = value.ToString(); break;
            case "serverName": tls.ServerName = value.ToString(); break;
        }
    }

    private static void SetQuicValue(QUICOptions quic, string key, object value)
    {
        switch (key)
        {
            case "keepaliveInterval": SetProp(quic, "KeepaliveInterval", Convert.ToInt32(value)); break;
            case "maxIdleTimeout": SetProp(quic, "MaxIdleTimeout", Convert.ToInt32(value)); break;
            case "maxIncomingStreams": SetProp(quic, "MaxIncomingStreams", Convert.ToInt32(value)); break;
        }
    }

    private static void SetLogValue(LogConfig log, string key, object value)
    {
        switch (key)
        {
            case "level": log.Level = value.ToString() ?? "info"; break;
            case "to": log.To = value.ToString(); break;
            case "maxDays": log.MaxDays = Convert.ToInt32(value); break;
            case "disablePrintColor": log.DisablePrintColor = Convert.ToBoolean(value); break;
        }
    }

    private static void SetWebServerValue(WebServerConfig ws, string key, object value)
    {
        switch (key)
        {
            case "addr": SetProp(ws, "Addr", value.ToString()); break;
            case "port": SetProp(ws, "Port", Convert.ToInt32(value)); break;
            case "user": SetProp(ws, "User", value.ToString()); break;
            case "password": SetProp(ws, "Password", value.ToString()); break;
            case "token": SetProp(ws, "Token", value.ToString()); break;
            case "pprofEnable": SetProp(ws, "PprofEnable", Convert.ToBoolean(value)); break;
        }
    }

    private static void SetVirtualNetValue(VirtualNetConfig vn, string key, object value)
    {
        switch (key)
        {
            case "address": SetProp(vn, "Address", value.ToString()); break;
            case "mtu": SetProp(vn, "Mtu", Convert.ToInt32(value)); break;
            case "routes" when value is List<string> list: SetProp(vn, "Routes", list); break;
        }
    }

    #endregion

    #region Proxy Setters

    private static void SetProxyContextValue(ProxyConfig proxy, string arraySub, string key, object value)
    {
        switch (arraySub)
        {
            case "":
                SetProxyValue(proxy, key, value);
                break;
            case "transport":
                proxy.Transport ??= new ProxyTransport();
                SetProxyTransportValue(proxy.Transport, key, value);
                break;
            case "loadBalancer":
                proxy.LoadBalancer ??= new LoadBalancerConfig { Group = "" };
                SetLoadBalancerValue(proxy.LoadBalancer, key, value);
                break;
            case "healthCheck":
                proxy.HealthCheck ??= new HealthCheckConfig { Type = "" };
                SetHealthCheckValue(proxy.HealthCheck, key, value);
                break;
            case "plugin":
                proxy.Plugin ??= new ClientPluginOptions { Type = "" };
                SetPluginValue(proxy.Plugin, key, value);
                break;
            case "natTraversal":
                proxy.NatTraversal ??= new NatTraversalConfig();
                SetNatTraversalValue(proxy.NatTraversal, key, value);
                break;
            case "requestHeaders":
                proxy.RequestHeaders ??= new HeaderOperations();
                SetHeaderOpsValue(proxy.RequestHeaders, key, value);
                break;
            case "responseHeaders":
                proxy.ResponseHeaders ??= new HeaderOperations();
                SetHeaderOpsValue(proxy.ResponseHeaders, key, value);
                break;
            case "metadata":
                proxy.Metadata ??= new Dictionary<string, string>();
                if (value is string s) proxy.Metadata[key] = s;
                break;
            case "annotations":
                proxy.Annotations ??= new Dictionary<string, string>();
                if (value is string sa) proxy.Annotations[key] = sa;
                break;
        }
    }

    private static void SetProxyValue(ProxyConfig proxy, string key, object value)
    {
        switch (key)
        {
            case "name": proxy.Name = value.ToString() ?? ""; break;
            case "type": proxy.Type = value.ToString() ?? "tcp"; break;
            case "localIP": proxy.LocalIP = value.ToString() ?? "127.0.0.1"; break;
            case "localPort": proxy.LocalPort = Convert.ToInt32(value); break;
            case "remotePort": proxy.RemotePort = Convert.ToInt32(value); break;
            case "customDomains": proxy.CustomDomains = (List<string>)value; break;
            case "subdomain": proxy.Subdomain = value.ToString(); break;
            case "locations": proxy.Locations = (List<string>)value; break;
            case "httpUser": proxy.HttpUser = value.ToString(); break;
            case "httpPassword": proxy.HttpPassword = value.ToString(); break;
            case "hostHeaderRewrite": proxy.HostHeaderRewrite = value.ToString(); break;
            case "routeByHttpUser": proxy.RouteByHttpUser = value.ToString(); break;
            case "secretKey": proxy.SecretKey = value.ToString(); break;
            case "allowUsers": proxy.AllowUsers = (List<string>)value; break;
            case "multiplexer": proxy.Multiplexer = value.ToString(); break;
        }
    }

    private static void SetProxyTransportValue(ProxyTransport transport, string key, object value)
    {
        switch (key)
        {
            case "useEncryption": SetProp(transport, "UseEncryption", Convert.ToBoolean(value)); break;
            case "useCompression": SetProp(transport, "UseCompression", Convert.ToBoolean(value)); break;
            case "bandwidthLimit": SetProp(transport, "BandwidthLimit", value.ToString()); break;
            case "bandwidthLimitMode": SetProp(transport, "BandwidthLimitMode", value.ToString()); break;
            case "proxyProtocolVersion": SetProp(transport, "ProxyProtocolVersion", value.ToString()); break;
        }
    }

    private static void SetLoadBalancerValue(LoadBalancerConfig lb, string key, object value)
    {
        switch (key)
        {
            case "group": SetProp(lb, "Group", value.ToString() ?? ""); break;
            case "groupKey": SetProp(lb, "GroupKey", value.ToString()); break;
        }
    }

    private static void SetHealthCheckValue(HealthCheckConfig hc, string key, object value)
    {
        switch (key)
        {
            case "type": SetProp(hc, "Type", value.ToString() ?? ""); break;
            case "timeoutSeconds": SetProp(hc, "TimeoutSeconds", Convert.ToInt32(value)); break;
            case "maxFailed": SetProp(hc, "MaxFailed", Convert.ToInt32(value)); break;
            case "intervalSeconds": SetProp(hc, "IntervalSeconds", Convert.ToInt32(value)); break;
            case "path": SetProp(hc, "Path", value.ToString()); break;
            case "httpHeaders" when value is List<Dictionary<string, string>> tables:
                var headers = tables
                    .Select(t => new HttpHeader
                        { Name = t.GetValueOrDefault("name", ""), Value = t.GetValueOrDefault("value", "") })
                    .ToList();
                SetProp(hc, "HttpHeaders", headers);
                break;
        }
    }

    private static void SetPluginValue(ClientPluginOptions plugin, string key, object value)
    {
        switch (key)
        {
            case "type": SetProp(plugin, "Type", value.ToString() ?? ""); break;
            case "httpProxyUrl": SetProp(plugin, "HttpProxyUrl", value.ToString()); break;
            case "socks5Url": SetProp(plugin, "Socks5Url", value.ToString()); break;
            case "staticFileLocalPath": SetProp(plugin, "StaticFileLocalPath", value.ToString()); break;
            case "staticFilePrefixUrl": SetProp(plugin, "StaticFilePrefixUrl", value.ToString()); break;
            case "https2httpLocalAddr": SetProp(plugin, "Https2HttpLocalAddr", value.ToString()); break;
            case "https2httpCrtPath": SetProp(plugin, "Https2HttpCrtPath", value.ToString()); break;
            case "https2httpKeyPath": SetProp(plugin, "Https2HttpKeyPath", value.ToString()); break;
            case "http2httpsLocalAddr": SetProp(plugin, "Http2HttpsLocalAddr", value.ToString()); break;
            case "http2httpsCrtPath": SetProp(plugin, "Http2HttpsCrtPath", value.ToString()); break;
            case "http2httpsKeyPath": SetProp(plugin, "Http2HttpsKeyPath", value.ToString()); break;
        }
    }

    private static void SetNatTraversalValue(NatTraversalConfig nt, string key, object value)
    {
        switch (key)
        {
            case "role": SetProp(nt, "Role", value.ToString()); break;
            case "keepaliveInterval": SetProp(nt, "KeepaliveInterval", Convert.ToInt32(value)); break;
        }
    }

    private static void SetHeaderOpsValue(HeaderOperations ho, string key, object value)
    {
        switch (key)
        {
            case "set" when value is bool b: SetProp(ho, "Set", (bool?)b); break;
            case "add" when value is Dictionary<string, string> dict: SetProp(ho, "Add", dict); break;
            case "remove" when value is List<string> list: SetProp(ho, "Remove", list); break;
        }
    }

    #endregion

    #region Visitor Setters

    private static void SetVisitorContextValue(VisitorConfig visitor, string arraySub, string key, object value)
    {
        switch (arraySub)
        {
            case "":
                SetVisitorValue(visitor, key, value);
                break;
            case "transport":
                visitor.Transport ??= new ClientTransportConfig();
                SetClientTransportValue(visitor.Transport, key, value);
                break;
            case "transport.tls":
                visitor.Transport ??= new ClientTransportConfig();
                visitor.Transport.Tls ??= new TLSClientConfig();
                SetTlsValue(visitor.Transport.Tls, key, value);
                break;
            case "transport.quic":
                visitor.Transport ??= new ClientTransportConfig();
                visitor.Transport.Quic ??= new QUICOptions();
                SetQuicValue(visitor.Transport.Quic, key, value);
                break;
            case "metadata":
                visitor.Metadata ??= new Dictionary<string, string>();
                if (value is string s) visitor.Metadata[key] = s;
                break;
        }
    }

    private static void SetVisitorValue(VisitorConfig visitor, string key, object value)
    {
        switch (key)
        {
            case "name": visitor.Name = value.ToString() ?? ""; break;
            case "type": visitor.Type = value.ToString() ?? "stcp"; break;
            case "serverName": visitor.ServerName = value.ToString() ?? ""; break;
            case "secretKey": visitor.SecretKey = value.ToString() ?? ""; break;
            case "bindAddr": visitor.BindAddr = value.ToString() ?? "127.0.0.1"; break;
            case "bindPort": visitor.BindPort = Convert.ToInt32(value); break;
            case "bindIp": visitor.BindIp = value.ToString(); break;
        }
    }

    #endregion

#pragma warning disable IL2075 // Reflection on init-only properties for TOML deserialization
    private static void SetProp(object obj, string propertyName, object? value)
    {
        obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(obj, value);
    }
#pragma warning restore IL2075

    #endregion

    #region Serialization

    private static string SerializeClientCommonConfig(ClientCommonConfig config)
    {
        var sb = new StringBuilder();

        // Top-level common fields MUST come before any [section]
        if (!string.IsNullOrEmpty(config.User))
        {
            sb.AppendLine($"user = \"{EscapeString(config.User)}\"");
        }

        if (config.ServerAddr != null)
        {
            sb.AppendLine($"serverAddr = \"{EscapeString(config.ServerAddr)}\"");
        }

        if (config.ServerPort != 7000)
        {
            sb.AppendLine($"serverPort = {config.ServerPort}");
        }

        if (!string.IsNullOrEmpty(config.NatHoleStunServer))
        {
            sb.AppendLine($"natHoleStunServer = \"{EscapeString(config.NatHoleStunServer)}\"");
        }

        if (!string.IsNullOrEmpty(config.DnsServer))
        {
            sb.AppendLine($"dnsServer = \"{EscapeString(config.DnsServer)}\"");
        }

        if (!config.LoginFailExit)
        {
            sb.AppendLine($"loginFailExit = {BoolStr(config.LoginFailExit)}");
        }

        if (config.Start is { Count: > 0 })
        {
            sb.AppendLine($"start = [{string.Join(", ", config.Start.Select(s => $"\"{s}\""))}]");
        }

        if (config.UdpPacketSize != 1500)
        {
            sb.AppendLine($"udpPacketSize = {config.UdpPacketSize}");
        }

        if (config.Includes is { Count: > 0 })
        {
            sb.AppendLine($"includes = [{string.Join(", ", config.Includes.Select(s => $"\"{s}\""))}]");
        }

        sb.AppendLine("");

        // Auth — only write if there's meaningful content
        if (config.Auth != null && (config.Auth.Method != "token" || !string.IsNullOrEmpty(config.Auth.Token)
                                                                  || config.Auth.AdditionalScopes is { Count: > 0 } ||
                                                                  config.Auth.TokenSource != null ||
                                                                  config.Auth.Oidc != null))
        {
            sb.AppendLine("[auth]");
            sb.AppendLine($"method = \"{EscapeString(config.Auth.Method)}\"");
            if (config.Auth.Token != null)
                sb.AppendLine($"token = \"{EscapeString(config.Auth.Token)}\"");
            if (config.Auth.AdditionalScopes is { Count: > 0 })
                sb.AppendLine(
                    $"additionalScopes = [{string.Join(", ", config.Auth.AdditionalScopes.Select(s => $"\"{s}\""))}]");

            if (config.Auth.TokenSource != null)
            {
                sb.AppendLine("[auth.tokenSource]");
                sb.AppendLine($"fileName = \"{EscapeString(config.Auth.TokenSource.FileName)}\"");
            }

            if (config.Auth.Oidc != null)
            {
                sb.AppendLine("[auth.oidc]");
                sb.AppendLine($"clientId = \"{EscapeString(config.Auth.Oidc.ClientId)}\"");
                sb.AppendLine($"clientSecret = \"{EscapeString(config.Auth.Oidc.ClientSecret)}\"");
                if (config.Auth.Oidc.Audience != null)
                {
                    sb.AppendLine($"audience = \"{EscapeString(config.Auth.Oidc.Audience)}\"");
                }

                if (config.Auth.Oidc.Scope != null)
                {
                    sb.AppendLine($"scope = \"{EscapeString(config.Auth.Oidc.Scope)}\"");
                }

                sb.AppendLine($"tokenEndpointUrl = \"{EscapeString(config.Auth.Oidc.TokenEndpointUrl)}\"");
                if (config.Auth.Oidc.AdditionalEndpointParams is { Count: > 0 })
                {
                    sb.AppendLine(
                        $"additionalEndpointParams = {SerializeInlineTable(config.Auth.Oidc.AdditionalEndpointParams)}");
                }

                if (config.Auth.Oidc.TrustedCaFile != null)
                {
                    sb.AppendLine($"trustedCaFile = \"{EscapeString(config.Auth.Oidc.TrustedCaFile)}\"");
                }

                if (config.Auth.Oidc.InsecureSkipVerify)
                {
                    sb.AppendLine("insecureSkipVerify = true");
                }

                if (config.Auth.Oidc.ProxyUrl != null)
                {
                    sb.AppendLine($"proxyUrl = \"{EscapeString(config.Auth.Oidc.ProxyUrl)}\"");
                }
            }
        }

        // Log — only write if non-default
        if (config.Log != null && (config.Log.Level != "info" || config.Log.To != null
                                                              || config.Log.MaxDays != 3 ||
                                                              config.Log.DisablePrintColor))
        {
            sb.AppendLine("[log]");
            if (config.Log.Level != "info")
            {
                sb.AppendLine($"level = \"{EscapeString(config.Log.Level)}\"");
            }

            if (config.Log.To != null)
            {
                sb.AppendLine($"to = \"{EscapeString(config.Log.To)}\"");
            }

            if (config.Log.MaxDays != 3)
            {
                sb.AppendLine($"maxDays = {config.Log.MaxDays}");
            }

            if (config.Log.DisablePrintColor)
            {
                sb.AppendLine("disablePrintColor = true");
            }
        }

        // WebServer
        if (config.WebServer != null)
        {
            sb.AppendLine("[webServer]");
            sb.AppendLine($"addr = \"{EscapeString(config.WebServer.Addr)}\"");
            sb.AppendLine($"port = {WebServerConfig.Port}");
            if (WebServerConfig.User != null)
            {
                sb.AppendLine($"user = \"{EscapeString(WebServerConfig.User)}\"");
            }

            if (WebServerConfig.Password != null)
            {
                sb.AppendLine($"password = \"{EscapeString(WebServerConfig.Password)}\"");
            }

            if (WebServerConfig.Token != null)
            {
                sb.AppendLine($"token = \"{EscapeString(WebServerConfig.Token)}\"");
            }

            if (WebServerConfig.PprofEnable)
            {
                sb.AppendLine("pprofEnable = true");
            }
        }

        // Transport — only write if non-default
        if (config.Transport != null)
        {
            var transportStr = SerializeClientTransport(config.Transport);
            if (!string.IsNullOrWhiteSpace(transportStr))
                sb.Append(transportStr);
        }

        // VirtualNet
        if (config.VirtualNet != null)
        {
            sb.AppendLine("[virtualNet]");
            sb.AppendLine($"address = \"{EscapeString(config.VirtualNet.Address)}\"");
            if (VirtualNetConfig.Mtu.HasValue)
                sb.AppendLine($"mtu = {VirtualNetConfig.Mtu.Value}");
            if (VirtualNetConfig.Routes is { Count: > 0 })
                sb.AppendLine(
                    $"routes = [{string.Join(", ", VirtualNetConfig.Routes.Select(r => $"\"{r}\""))}]");
        }

        // Metadata
        if (config.Metadata is { Count: > 0 })
        {
            sb.AppendLine("[metadata]");
            foreach (var (k, v) in config.Metadata)
                sb.AppendLine($"{k} = \"{EscapeString(v)}\"");
        }

        // FeatureGates
        if (config.FeatureGates is not { Count: > 0 }) return sb.ToString().TrimEnd();
        sb.AppendLine("[featureGates]");
        foreach (var (k, v) in config.FeatureGates)
            sb.AppendLine($"{k} = {BoolStr(v)}");

        return sb.ToString().TrimEnd();
    }

    private static string SerializeClientTransport(ClientTransportConfig transport)
    {
        // Build transport content; only output [transport] if there's non-default content
        var content = new StringBuilder();

        if (transport.Protocol != "tcp")
            content.AppendLine($"protocol = \"{EscapeString(transport.Protocol)}\"");
        if (transport.DialServerTimeout != 10)
            content.AppendLine($"dialServerTimeout = {transport.DialServerTimeout}");
        if (transport.DialServerKeepalive.HasValue)
            content.AppendLine($"dialServerKeepalive = {transport.DialServerKeepalive.Value}");
        if (transport.ConnectServerLocalIP != null)
            content.AppendLine($"connectServerLocalIP = \"{EscapeString(transport.ConnectServerLocalIP)}\"");
        if (transport.ProxyUrl != null)
            content.AppendLine($"proxyUrl = \"{EscapeString(transport.ProxyUrl)}\"");
        if (transport.PoolCount.HasValue)
            content.AppendLine($"poolCount = {transport.PoolCount.Value}");
        if (!transport.TcpMux)
            content.AppendLine($"tcpMux = {BoolStr(transport.TcpMux)}");
        if (transport.TcpMuxKeepaliveInterval.HasValue)
            content.AppendLine($"tcpMuxKeepaliveInterval = {transport.TcpMuxKeepaliveInterval.Value}");
        if (transport.HeartbeatInterval != 30)
            content.AppendLine($"heartbeatInterval = {transport.HeartbeatInterval}");
        if (transport.HeartbeatTimeout != 90)
            content.AppendLine($"heartbeatTimeout = {transport.HeartbeatTimeout}");
        if (transport.UseEncryption)
            content.AppendLine("useEncryption = true");
        if (transport.UseCompression)
            content.AppendLine("useCompression = true");

        var sb = new StringBuilder();

        // Only write [transport] header if there are transport-level fields or sub-sections
        var hasTransportFields = content.Length > 0;
        var hasTls = transport.Tls != null && (!transport.Tls.Enable
                                               || transport.Tls.DisableCustomTLSFirstByte ||
                                               transport.Tls.CertFile != null
                                               || transport.Tls.KeyFile != null || transport.Tls.CaFile != null ||
                                               transport.Tls.ServerName != null);
        var hasQuic = transport.Quic != null && (QUICOptions.KeepaliveInterval.HasValue
                                                 || QUICOptions.MaxIdleTimeout.HasValue ||
                                                 QUICOptions.MaxIncomingStreams.HasValue);

        if (!hasTransportFields && !hasTls && !hasQuic)
            return "";

        sb.AppendLine("[transport]");
        if (content.Length > 0)
            sb.Append(content);

        if (transport.Tls != null && hasTls)
        {
            sb.AppendLine("[transport.tls]");
            if (!transport.Tls.Enable)
                sb.AppendLine($"enable = {BoolStr(transport.Tls.Enable)}");
            if (transport.Tls.DisableCustomTLSFirstByte)
                sb.AppendLine("disableCustomTLSFirstByte = true");
            if (transport.Tls.CertFile != null)
                sb.AppendLine($"certFile = \"{EscapeString(transport.Tls.CertFile)}\"");
            if (transport.Tls.KeyFile != null)
                sb.AppendLine($"keyFile = \"{EscapeString(transport.Tls.KeyFile)}\"");
            if (transport.Tls.CaFile != null)
                sb.AppendLine($"caFile = \"{EscapeString(transport.Tls.CaFile)}\"");
            if (transport.Tls.ServerName != null)
                sb.AppendLine($"serverName = \"{EscapeString(transport.Tls.ServerName)}\"");
        }

        if (transport.Quic == null || !hasQuic) return sb.ToString().TrimEnd();

        sb.AppendLine("[transport.quic]");
        if (QUICOptions.KeepaliveInterval.HasValue)
            sb.AppendLine($"keepaliveInterval = {QUICOptions.KeepaliveInterval.Value}");
        if (QUICOptions.MaxIdleTimeout.HasValue)
            sb.AppendLine($"maxIdleTimeout = {QUICOptions.MaxIdleTimeout.Value}");
        if (QUICOptions.MaxIncomingStreams.HasValue)
            sb.AppendLine($"maxIncomingStreams = {QUICOptions.MaxIncomingStreams.Value}");

        return sb.ToString().TrimEnd();
    }

    private static string SerializeProxy(ProxyConfig proxy)
    {
        var sb = new StringBuilder();

        // Basic fields
        sb.AppendLine($"name = \"{EscapeString(proxy.Name)}\"");
        sb.AppendLine($"type = \"{EscapeString(proxy.Type)}\"");
        sb.AppendLine($"localIP = \"{EscapeString(proxy.LocalIP)}\"");
        sb.AppendLine($"localPort = {proxy.LocalPort}");

        if (proxy.RemotePort.HasValue)
            sb.AppendLine($"remotePort = {proxy.RemotePort.Value}");
        if (proxy.CustomDomains is { Count: > 0 })
            sb.AppendLine(
                $"customDomains = [{string.Join(", ", proxy.CustomDomains.Select(d => $"\"{d}\""))}]");
        if (!string.IsNullOrEmpty(proxy.Subdomain))
            sb.AppendLine($"subdomain = \"{EscapeString(proxy.Subdomain)}\"");
        if (proxy.Locations is { Count: > 0 })
            sb.AppendLine($"locations = [{string.Join(", ", proxy.Locations.Select(l => $"\"{l}\""))}]");
        if (!string.IsNullOrEmpty(proxy.HttpUser))
            sb.AppendLine($"httpUser = \"{EscapeString(proxy.HttpUser)}\"");
        if (!string.IsNullOrEmpty(proxy.HttpPassword))
            sb.AppendLine($"httpPassword = \"{EscapeString(proxy.HttpPassword)}\"");
        if (!string.IsNullOrEmpty(proxy.HostHeaderRewrite))
            sb.AppendLine($"hostHeaderRewrite = \"{EscapeString(proxy.HostHeaderRewrite)}\"");
        if (!string.IsNullOrEmpty(proxy.RouteByHttpUser))
            sb.AppendLine($"routeByHttpUser = \"{EscapeString(proxy.RouteByHttpUser)}\"");
        if (!string.IsNullOrEmpty(proxy.SecretKey))
            sb.AppendLine($"secretKey = \"{EscapeString(proxy.SecretKey)}\"");
        if (proxy.AllowUsers is { Count: > 0 })
            sb.AppendLine($"allowUsers = [{string.Join(", ", proxy.AllowUsers.Select(u => $"\"{u}\""))}]");
        if (!string.IsNullOrEmpty(proxy.Multiplexer))
            sb.AppendLine($"multiplexer = \"{EscapeString(proxy.Multiplexer)}\"");

        // Transport
        if (proxy.Transport != null)
        {
            var t = proxy.Transport;
            var hasValues = t.UseEncryption || t.UseCompression ||
                            !string.IsNullOrEmpty(t.BandwidthLimit) ||
                            !string.IsNullOrEmpty(t.BandwidthLimitMode) ||
                            !string.IsNullOrEmpty(t.ProxyProtocolVersion);
            if (hasValues)
            {
                sb.AppendLine();
                sb.AppendLine("[proxies.transport]");
                if (t.UseEncryption)
                    sb.AppendLine("useEncryption = true");
                if (t.UseCompression)
                    sb.AppendLine("useCompression = true");
                if (!string.IsNullOrEmpty(t.BandwidthLimit))
                    sb.AppendLine($"bandwidthLimit = \"{EscapeString(t.BandwidthLimit)}\"");
                if (!string.IsNullOrEmpty(t.BandwidthLimitMode))
                    sb.AppendLine($"bandwidthLimitMode = \"{EscapeString(t.BandwidthLimitMode)}\"");
                if (!string.IsNullOrEmpty(t.ProxyProtocolVersion))
                    sb.AppendLine($"proxyProtocolVersion = \"{EscapeString(t.ProxyProtocolVersion)}\"");
            }
        }

        // LoadBalancer
        if (proxy.LoadBalancer != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.loadBalancer]");
            sb.AppendLine($"group = \"{EscapeString(proxy.LoadBalancer.Group)}\"");
            if (!string.IsNullOrEmpty(proxy.LoadBalancer.GroupKey))
                sb.AppendLine($"groupKey = \"{EscapeString(proxy.LoadBalancer.GroupKey)}\"");
        }

        // HealthCheck
        if (proxy.HealthCheck != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.healthCheck]");
            sb.AppendLine($"type = \"{EscapeString(proxy.HealthCheck.Type)}\"");
            if (proxy.HealthCheck.TimeoutSeconds != 3)
                sb.AppendLine($"timeoutSeconds = {proxy.HealthCheck.TimeoutSeconds}");
            if (proxy.HealthCheck.MaxFailed != 1)
                sb.AppendLine($"maxFailed = {proxy.HealthCheck.MaxFailed}");
            if (proxy.HealthCheck.IntervalSeconds != 10)
                sb.AppendLine($"intervalSeconds = {proxy.HealthCheck.IntervalSeconds}");
            if (!string.IsNullOrEmpty(proxy.HealthCheck.Path))
                sb.AppendLine($"path = \"{EscapeString(proxy.HealthCheck.Path)}\"");
            if (proxy.HealthCheck.HttpHeaders is { Count: > 0 })
            {
                var headers = string.Join(", ", proxy.HealthCheck.HttpHeaders.Select(h =>
                    $"{{name = \"{EscapeString(h.Name)}\", value = \"{EscapeString(h.Value)}\"}}"));
                sb.AppendLine($"httpHeaders = [{headers}]");
            }
        }

        // Plugin
        if (proxy.Plugin != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.plugin]");
            sb.AppendLine($"type = \"{EscapeString(proxy.Plugin.Type)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.HttpProxyUrl))
                sb.AppendLine($"httpProxyUrl = \"{EscapeString(proxy.Plugin.HttpProxyUrl)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Socks5Url))
                sb.AppendLine($"socks5Url = \"{EscapeString(proxy.Plugin.Socks5Url)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.StaticFileLocalPath))
                sb.AppendLine($"staticFileLocalPath = \"{EscapeString(proxy.Plugin.StaticFileLocalPath)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.StaticFilePrefixUrl))
                sb.AppendLine($"staticFilePrefixUrl = \"{EscapeString(proxy.Plugin.StaticFilePrefixUrl)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Https2HttpLocalAddr))
                sb.AppendLine($"https2httpLocalAddr = \"{EscapeString(proxy.Plugin.Https2HttpLocalAddr)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Https2HttpCrtPath))
                sb.AppendLine($"https2httpCrtPath = \"{EscapeString(proxy.Plugin.Https2HttpCrtPath)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Https2HttpKeyPath))
                sb.AppendLine($"https2httpKeyPath = \"{EscapeString(proxy.Plugin.Https2HttpKeyPath)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Http2HttpsLocalAddr))
                sb.AppendLine($"http2httpsLocalAddr = \"{EscapeString(proxy.Plugin.Http2HttpsLocalAddr)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Http2HttpsCrtPath))
                sb.AppendLine($"http2httpsCrtPath = \"{EscapeString(proxy.Plugin.Http2HttpsCrtPath)}\"");
            if (!string.IsNullOrEmpty(proxy.Plugin.Http2HttpsKeyPath))
                sb.AppendLine($"http2httpsKeyPath = \"{EscapeString(proxy.Plugin.Http2HttpsKeyPath)}\"");
        }

        // NatTraversal
        if (proxy.NatTraversal != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.natTraversal]");
            if (!string.IsNullOrEmpty(proxy.NatTraversal.Role))
                sb.AppendLine($"role = \"{EscapeString(proxy.NatTraversal.Role)}\"");
            if (proxy.NatTraversal.KeepaliveInterval.HasValue)
                sb.AppendLine($"keepaliveInterval = {proxy.NatTraversal.KeepaliveInterval.Value}");
        }

        // RequestHeaders
        if (proxy.RequestHeaders != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.requestHeaders]");
            SerializeHeaderOperations(sb, proxy.RequestHeaders);
        }

        // ResponseHeaders
        if (proxy.ResponseHeaders != null)
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.responseHeaders]");
            SerializeHeaderOperations(sb, proxy.ResponseHeaders);
        }

        // Metadata
        if (proxy.Metadata is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("[proxies.metadata]");
            foreach (var (k, v) in proxy.Metadata)
                sb.AppendLine($"{k} = \"{EscapeString(v)}\"");
        }

        // Annotations
        if (proxy.Annotations is not { Count: > 0 }) return sb.ToString().TrimEnd();

        sb.AppendLine();
        sb.AppendLine("[proxies.annotations]");
        foreach (var (k, v) in proxy.Annotations)
            sb.AppendLine($"{k} = \"{EscapeString(v)}\"");

        return sb.ToString().TrimEnd();
    }

    private static string SerializeVisitor(VisitorConfig visitor)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"name = \"{EscapeString(visitor.Name)}\"");
        sb.AppendLine($"type = \"{EscapeString(visitor.Type)}\"");
        sb.AppendLine($"serverName = \"{EscapeString(visitor.ServerName)}\"");
        sb.AppendLine($"secretKey = \"{EscapeString(visitor.SecretKey)}\"");
        sb.AppendLine($"bindAddr = \"{EscapeString(visitor.BindAddr)}\"");
        sb.AppendLine($"bindPort = {visitor.BindPort}");

        if (!string.IsNullOrEmpty(visitor.BindIp))
            sb.AppendLine($"bindIp = \"{EscapeString(visitor.BindIp)}\"");

        // Transport
        if (visitor.Transport != null)
        {
            sb.AppendLine();
            sb.Append(SerializeVisitorTransport(visitor.Transport));
        }

        // Metadata
        if (visitor.Metadata is not { Count: > 0 }) return sb.ToString().TrimEnd();

        sb.AppendLine();
        sb.AppendLine("[visitors.metadata]");
        foreach (var (k, v) in visitor.Metadata)
            sb.AppendLine($"{k} = \"{EscapeString(v)}\"");

        return sb.ToString().TrimEnd();
    }

    private static string SerializeVisitorTransport(ClientTransportConfig transport)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[visitors.transport]");

        if (transport.Protocol != "tcp")
            sb.AppendLine($"protocol = \"{EscapeString(transport.Protocol)}\"");
        if (transport.DialServerTimeout != 10)
            sb.AppendLine($"dialServerTimeout = {transport.DialServerTimeout}");
        if (transport.DialServerKeepalive.HasValue)
            sb.AppendLine($"dialServerKeepalive = {transport.DialServerKeepalive.Value}");
        if (transport.ConnectServerLocalIP != null)
            sb.AppendLine($"connectServerLocalIP = \"{EscapeString(transport.ConnectServerLocalIP)}\"");
        if (transport.ProxyUrl != null)
            sb.AppendLine($"proxyUrl = \"{EscapeString(transport.ProxyUrl)}\"");
        if (transport.PoolCount.HasValue)
            sb.AppendLine($"poolCount = {transport.PoolCount.Value}");
        if (!transport.TcpMux)
            sb.AppendLine($"tcpMux = {BoolStr(transport.TcpMux)}");
        if (transport.TcpMuxKeepaliveInterval.HasValue)
            sb.AppendLine($"tcpMuxKeepaliveInterval = {transport.TcpMuxKeepaliveInterval.Value}");
        if (transport.HeartbeatInterval != 30)
            sb.AppendLine($"heartbeatInterval = {transport.HeartbeatInterval}");
        if (transport.HeartbeatTimeout != 90)
            sb.AppendLine($"heartbeatTimeout = {transport.HeartbeatTimeout}");
        if (transport.UseEncryption)
            sb.AppendLine("useEncryption = true");
        if (transport.UseCompression)
            sb.AppendLine("useCompression = true");

        if (transport.Tls != null)
        {
            sb.AppendLine("[visitors.transport.tls]");
            if (!transport.Tls.Enable)
                sb.AppendLine($"enable = {BoolStr(transport.Tls.Enable)}");
            if (transport.Tls.DisableCustomTLSFirstByte)
                sb.AppendLine("disableCustomTLSFirstByte = true");
            if (transport.Tls.CertFile != null)
                sb.AppendLine($"certFile = \"{EscapeString(transport.Tls.CertFile)}\"");
            if (transport.Tls.KeyFile != null)
                sb.AppendLine($"keyFile = \"{EscapeString(transport.Tls.KeyFile)}\"");
            if (transport.Tls.CaFile != null)
                sb.AppendLine($"caFile = \"{EscapeString(transport.Tls.CaFile)}\"");
            if (transport.Tls.ServerName != null)
                sb.AppendLine($"serverName = \"{EscapeString(transport.Tls.ServerName)}\"");
        }

        if (transport.Quic == null) return sb.ToString().TrimEnd();
        sb.AppendLine("[visitors.transport.quic]");
        if (QUICOptions.KeepaliveInterval.HasValue)
            sb.AppendLine($"keepaliveInterval = {QUICOptions.KeepaliveInterval.Value}");
        if (QUICOptions.MaxIdleTimeout.HasValue)
            sb.AppendLine($"maxIdleTimeout = {QUICOptions.MaxIdleTimeout.Value}");
        if (QUICOptions.MaxIncomingStreams.HasValue)
            sb.AppendLine($"maxIncomingStreams = {QUICOptions.MaxIncomingStreams.Value}");

        return sb.ToString().TrimEnd();
    }

    private static void SerializeHeaderOperations(StringBuilder sb, HeaderOperations headers)
    {
        if (headers.Set.HasValue)
            sb.AppendLine($"set = {BoolStr(headers.Set.Value)}");
        if (headers.Add is { Count: > 0 })
            sb.AppendLine($"add = {SerializeInlineTable(headers.Add)}");
        if (headers.Remove is { Count: > 0 })
            sb.AppendLine($"remove = [{string.Join(", ", headers.Remove.Select(r => $"\"{r}\""))}]");
    }

    #endregion

    #region Helpers

    private static string SerializeInlineTable(Dictionary<string, string> dict)
    {
        return "{" + string.Join(", ", dict.Select(kv => $"{kv.Key} = \"{EscapeString(kv.Value)}\"")) + "}";
    }

    private static string EscapeString(string? value)
    {
        return value == null ? "" : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string BoolStr(bool value) => value ? "true" : "false";

    #endregion
}