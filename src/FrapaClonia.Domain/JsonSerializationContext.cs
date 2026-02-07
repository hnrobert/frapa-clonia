using System.Text.Json.Serialization;
using FrapaClonia.Domain.Models;

namespace FrapaClonia.Domain;

/// <summary>
/// JSON serialization context for Native AOT compatibility
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FrpClientConfig))]
[JsonSerializable(typeof(ClientCommonConfig))]
[JsonSerializable(typeof(AuthConfig))]
[JsonSerializable(typeof(ValueSource))]
[JsonSerializable(typeof(AuthOIDCClientConfig))]
[JsonSerializable(typeof(LogConfig))]
[JsonSerializable(typeof(WebServerConfig))]
[JsonSerializable(typeof(ClientTransportConfig))]
[JsonSerializable(typeof(QUICOptions))]
[JsonSerializable(typeof(TLSClientConfig))]
[JsonSerializable(typeof(VirtualNetConfig))]
[JsonSerializable(typeof(ProxyConfig))]
[JsonSerializable(typeof(ProxyTransport))]
[JsonSerializable(typeof(ProxyBackend))]
[JsonSerializable(typeof(LoadBalancerConfig))]
[JsonSerializable(typeof(HealthCheckConfig))]
[JsonSerializable(typeof(HttpHeader))]
[JsonSerializable(typeof(HeaderOperations))]
[JsonSerializable(typeof(ClientPluginOptions))]
[JsonSerializable(typeof(NatTraversalConfig))]
[JsonSerializable(typeof(VisitorConfig))]
[JsonSerializable(typeof(List<ProxyConfig>))]
[JsonSerializable(typeof(List<VisitorConfig>))]
[JsonSerializable(typeof(Dictionary<string, bool>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
public partial class FrpClientConfigContext : JsonSerializerContext
{
}
