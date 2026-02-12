using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FrapaClonia.Infrastructure.Services;

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
                logger.LogInformation("TOML file not found at {FilePath}, returning empty config", filePath);
                return Task.FromResult<FrpClientConfig?>(new FrpClientConfig
                {
                    CommonConfig = new ClientCommonConfig(),
                    Proxies = new List<ProxyConfig>(),
                    Visitors = new List<VisitorConfig>()
                });
            }

            var tomlContent = File.ReadAllText(filePath, Encoding.UTF8);
            logger.LogInformation("Deserializing TOML file at {FilePath}", filePath);
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
                sb.AppendLine(SerializeClientCommonConfig(configuration.CommonConfig));
                sb.AppendLine();
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

    public Task SerializeToFileAsync(string filePath, FrpClientConfig configuration, CancellationToken cancellationToken = default)
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
            logger.LogInformation("Serialized configuration to file at {FilePath}", filePath);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing TOML file at {FilePath}", filePath);
            return Task.CompletedTask;
        }
    }

    private FrpClientConfig ParseToml(string tomlContent)
    {
        var config = new FrpClientConfig
        {
            CommonConfig = new ClientCommonConfig(),
            Proxies = new List<ProxyConfig>(),
            Visitors = new List<VisitorConfig>()
        };

        var lines = tomlContent.Split('\n');
        var currentSection = "";
        var inArraySection = false;
        var arrayName = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            // Section header
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                if (trimmed.StartsWith("[["))
                {
                    // Array section
                    arrayName = trimmed[2..^1].Trim();
                    inArraySection = true;

                    if (arrayName == "proxies")
                    {
                        config.Proxies.Add(new ProxyConfig { Name = "", Type = "tcp", LocalIP = "127.0.0.1" });
                    }
                    else if (arrayName == "visitors")
                    {
                        config.Visitors.Add(new VisitorConfig { Name = "", Type = "stcp", BindAddr = "127.0.0.1" });
                    }
                }
                else
                {
                    currentSection = trimmed[1..^1].Trim();
                    inArraySection = false;
                }
                continue;
            }

            // Key-value pair
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = trimmed[..eqIndex].Trim();
                var value = trimmed[(eqIndex + 1)..].Trim();

                // Remove quotes from string values
                if (value.StartsWith('"') && value.EndsWith('"'))
                    value = value[1..^1];

                // Parse array values
                if (value.StartsWith('[') && value.EndsWith(']'))
                {
                    value = value[1..^1];
                    var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(v => v.Trim('"')).ToList();

                    SetConfigValue(config, currentSection, arrayName, inArraySection, key, values);
                }
                // Parse boolean
                else if (bool.TryParse(value, out var boolVal))
                {
                    SetConfigValue(config, currentSection, arrayName, inArraySection, key, boolVal);
                }
                // Parse integer
                else if (int.TryParse(value, out var intVal))
                {
                    SetConfigValue(config, currentSection, arrayName, inArraySection, key, intVal);
                }
                // String value
                else
                {
                    SetConfigValue(config, currentSection, arrayName, inArraySection, key, value);
                }
            }
        }

        return config;
    }

    private static void SetConfigValue(FrpClientConfig config, string section, string arrayName, bool inArray, string key, object value)
    {
        if (inArray)
        {
            if (arrayName == "proxies" && config.Proxies.Count > 0)
            {
                var proxy = config.Proxies[^1];
                SetProxyValue(proxy, key, value);
            }
            else if (arrayName == "visitors" && config.Visitors.Count > 0)
            {
                var visitor = config.Visitors[^1];
                SetVisitorValue(visitor, key, value);
            }
        }
        else
        {
            if (section == "auth")
            {
                config.CommonConfig!.Auth ??= new AuthConfig();
                SetAuthValue(config.CommonConfig.Auth, key, value);
            }
            else
            {
                SetCommonConfigValue(config.CommonConfig!, key, value);
            }
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
            case "customDomains": proxy.CustomDomains = ((List<string>)value); break;
            case "subdomain": proxy.Subdomain = value.ToString(); break;
            case "secretKey": proxy.SecretKey = value.ToString(); break;
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

    private static void SetCommonConfigValue(ClientCommonConfig config, string key, object value)
    {
        switch (key)
        {
            case "user": config.User = value.ToString(); break;
            case "serverAddr": config.ServerAddr = value.ToString(); break;
            case "serverPort": config.ServerPort = Convert.ToInt32(value); break;
            case "loginFailExit": config.LoginFailExit = Convert.ToBoolean(value); break;
        }
    }

    private static string SerializeClientCommonConfig(ClientCommonConfig config)
    {
        var sb = new StringBuilder();

        if (config.Auth != null)
        {
            sb.AppendLine("[auth]");
            sb.AppendLine($"method = \"{EscapeString(config.Auth.Method)}\"");
            if (config.Auth.Token != null)
                sb.AppendLine($"token = \"{EscapeString(config.Auth.Token)}\"");
            if (config.Auth.AdditionalScopes != null && config.Auth.AdditionalScopes.Count > 0)
                sb.AppendLine($"additionalScopes = [{string.Join(", ", config.Auth.AdditionalScopes.Select(s => $"\"{s}\""))}]");
        }

        if (!string.IsNullOrEmpty(config.User))
            sb.AppendLine($"user = \"{EscapeString(config.User)}\"");
        if (config.ServerAddr != null)
            sb.AppendLine($"serverAddr = \"{EscapeString(config.ServerAddr)}\"");
        if (config.ServerPort != 7000)
            sb.AppendLine($"serverPort = {config.ServerPort}");

        return sb.ToString().TrimEnd();
    }

    private static string SerializeProxy(ProxyConfig proxy)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"name = \"{EscapeString(proxy.Name)}\"");
        sb.AppendLine($"type = \"{EscapeString(proxy.Type)}\"");
        sb.AppendLine($"localIP = \"{EscapeString(proxy.LocalIP)}\"");
        sb.AppendLine($"localPort = {proxy.LocalPort}");

        if (proxy.RemotePort.HasValue)
            sb.AppendLine($"remotePort = {proxy.RemotePort.Value}");

        if (proxy.CustomDomains != null && proxy.CustomDomains.Count > 0)
            sb.AppendLine($"customDomains = [{string.Join(", ", proxy.CustomDomains.Select(d => $"\"{d}\""))}]");

        if (!string.IsNullOrEmpty(proxy.Subdomain))
            sb.AppendLine($"subdomain = \"{EscapeString(proxy.Subdomain)}\"");

        if (proxy.SecretKey != null)
            sb.AppendLine($"secretKey = \"{EscapeString(proxy.SecretKey)}\"");

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

        return sb.ToString().TrimEnd();
    }

    private static string EscapeString(string? value)
    {
        if (value == null) return "";
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
