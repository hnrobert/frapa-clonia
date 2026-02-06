using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for validating frp configurations
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateConfiguration(FrpClientConfig configuration)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (configuration.CommonConfig == null)
        {
            return ValidationResult.Failure("Common configuration is required");
        }

        var serverValidation = ValidateServerConnection(configuration.CommonConfig);
        errors.AddRange(serverValidation.Errors);
        warnings.AddRange(serverValidation.Warnings);

        foreach (var proxy in configuration.Proxies)
        {
            var proxyValidation = ValidateProxy(proxy);
            errors.AddRange(proxyValidation.Errors);
            warnings.AddRange(proxyValidation.Warnings);
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public ValidationResult ValidateProxy(ProxyConfig proxy)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Name validation
        if (string.IsNullOrWhiteSpace(proxy.Name))
        {
            errors.Add("Proxy name is required");
        }

        // Type validation
        var validTypes = new[] { "tcp", "udp", "http", "https", "tcpmux", "stcp", "sudp", "xtcp" };
        if (string.IsNullOrWhiteSpace(proxy.Type) || !validTypes.Contains(proxy.Type.ToLower()))
        {
            errors.Add($"Proxy type must be one of: {string.Join(", ", validTypes)}");
        }

        // Port validation
        if (proxy.LocalPort <= 0 || proxy.LocalPort > 65535)
        {
            errors.Add($"Invalid local port: {proxy.LocalPort}. Port must be between 1 and 65535");
        }

        // Type-specific validation
        var type = proxy.Type?.ToLower() ?? "";
        switch (type)
        {
            case "tcp":
            case "udp":
                if (!proxy.RemotePort.HasValue || proxy.RemotePort.Value <= 0)
                {
                    errors.Add($"{type.ToUpper()} proxy requires remotePort to be specified");
                }
                break;

            case "http":
            case "https":
            case "tcpmux":
                if ((proxy.CustomDomains == null || proxy.CustomDomains.Count == 0) && string.IsNullOrWhiteSpace(proxy.Subdomain))
                {
                    warnings.Add($"{type.ToUpper()} proxy should have customDomains or subdomain specified");
                }
                break;

            case "stcp":
            case "xtcp":
            case "sudp":
                if (string.IsNullOrWhiteSpace(proxy.SecretKey))
                {
                    errors.Add($"{type.ToUpper()} proxy requires secretKey to be specified");
                }
                break;
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    public ValidationResult ValidateServerConnection(ClientCommonConfig serverConfig)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Server address validation
        if (string.IsNullOrWhiteSpace(serverConfig.ServerAddr))
        {
            errors.Add("Server address is required");
        }
        else
        {
            // Check if it's a valid IP or hostname
            if (!IsValidIpAddress(serverConfig.ServerAddr) && !IsValidHostname(serverConfig.ServerAddr))
            {
                errors.Add("Server address must be a valid IP address or hostname");
            }
        }

        // Server port validation
        if (serverConfig.ServerPort <= 0 || serverConfig.ServerPort > 65535)
        {
            errors.Add($"Invalid server port: {serverConfig.ServerPort}. Port must be between 1 and 65535");
        }

        // Token validation for token authentication
        if (serverConfig.Auth?.Method == "token" && string.IsNullOrWhiteSpace(serverConfig.Auth.Token))
        {
            warnings.Add("Token authentication is configured but no token is provided");
        }

        // OIDC validation
        if (serverConfig.Auth?.Method == "oidc")
        {
            if (string.IsNullOrWhiteSpace(serverConfig.Auth.Oidc?.ClientId))
            {
                errors.Add("OIDC authentication requires clientId");
            }
            if (string.IsNullOrWhiteSpace(serverConfig.Auth.Oidc?.ClientSecret))
            {
                errors.Add("OIDC authentication requires clientSecret");
            }
            if (string.IsNullOrWhiteSpace(serverConfig.Auth.Oidc?.TokenEndpointUrl))
            {
                errors.Add("OIDC authentication requires tokenEndpointUrl");
            }
        }

        // Transport validation
        if (serverConfig.Transport != null)
        {
            var validProtocols = new[] { "tcp", "kcp", "quic", "websocket", "wss" };
            if (!string.IsNullOrWhiteSpace(serverConfig.Transport.Protocol) &&
                !validProtocols.Contains(serverConfig.Transport.Protocol.ToLower()))
            {
                warnings.Add($"Unknown transport protocol: {serverConfig.Transport.Protocol}");
            }

            if (serverConfig.Transport.HeartbeatInterval > 0 && serverConfig.Transport.HeartbeatTimeout > 0 &&
                serverConfig.Transport.HeartbeatTimeout <= serverConfig.Transport.HeartbeatInterval)
            {
                warnings.Add("Heartbeat timeout should be greater than heartbeat interval");
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private bool IsValidIpAddress(string ip)
    {
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    private bool IsValidHostname(string hostname)
    {
        if (hostname.Length > 253)
            return false;

        var parts = hostname.Split('.');
        if (parts.Length < 1)
            return false;

        return parts.All(part =>
        {
            if (part.Length < 1 || part.Length > 63)
                return false;

            // Each part must start and end with alphanumeric character
            // and can contain hyphens in between
            return !part.StartsWith('-') && !part.EndsWith('-') &&
                   part.All(c => char.IsLetterOrDigit(c) || c == '-');
        });
    }
}
