using FrapaClonia.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Provides localized string resources as dynamic properties for XAML binding
/// </summary>
public class LocalizedResources : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly Dictionary<string, LocalizedString> _strings = new();

    public LocalizedResources(ILocalizationService localizationService)
    {
        _localizationService = localizationService;

        // Initialize all localized strings
        AppTitle = Create("AppTitle");
        Dashboard = Create("Dashboard");
        ServerConfig = Create("ServerConfig");
        ProxyManagement = Create("ProxyManagement");
        Deployment = Create("Deployment");
        Settings = Create("Settings");
        Logs = Create("Logs");
        About = Create("About");
        StartFrpc = Create("StartFrpc");
        StopFrpc = Create("StopFrpc");
        RestartFrpc = Create("RestartFrpc");
        AddProxy = Create("AddProxy");
        EditProxy = Create("EditProxy");
        DeleteProxy = Create("DeleteProxy");
        DuplicateProxy = Create("DuplicateProxy");
        ClearAll = Create("ClearAll");
        Save = Create("Save");
        Cancel = Create("Cancel");
        Refresh = Create("Refresh");
        Export = Create("Export");
        Import = Create("Import");
        ProxyName = Create("ProxyName");
        ProxyType = Create("ProxyType");
        LocalIP = Create("LocalIP");
        LocalPort = Create("LocalPort");
        RemotePort = Create("RemotePort");
        CustomDomains = Create("CustomDomains");
        Subdomain = Create("Subdomain");
        SecretKey = Create("SecretKey");
        ServerAddress = Create("ServerAddress");
        ServerPort = Create("ServerPort");
        Token = Create("Token");
        OIDC = Create("OIDC");
        TransportProtocol = Create("TransportProtocol");
        EnableTLS = Create("EnableTLS");
        HealthCheck = Create("HealthCheck");
        Plugin = Create("Plugin");
        EnableEncryption = Create("EnableEncryption");
        EnableCompression = Create("EnableCompression");
        FrpcStatus = Create("FrpcStatus");
        FrpcRunning = Create("FrpcRunning");
        FrpcNotRunning = Create("FrpcNotRunning");
        ProcessId = Create("ProcessId");
        Configuration = Create("Configuration");
        Visitors = Create("Visitors");
        AddVisitor = Create("AddVisitor");
        EditVisitor = Create("EditVisitor");
        DeleteVisitor = Create("DeleteVisitor");
        VisitorName = Create("VisitorName");
        VisitorType = Create("VisitorType");
        ServerName = Create("ServerName");
        BindAddr = Create("BindAddr");
        BindPort = Create("BindPort");
        Language = Create("Language");
        AutoStart = Create("AutoStart");
        PortableMode = Create("PortableMode");
        QuickShare = Create("QuickShare");
    }

    // Localized string properties
    public LocalizedString AppTitle { get; }
    public LocalizedString Dashboard { get; }
    public LocalizedString ServerConfig { get; }
    public LocalizedString ProxyManagement { get; }
    public LocalizedString Deployment { get; }
    public LocalizedString Settings { get; }
    public LocalizedString Logs { get; }
    public LocalizedString About { get; }
    public LocalizedString StartFrpc { get; }
    public LocalizedString StopFrpc { get; }
    public LocalizedString RestartFrpc { get; }
    public LocalizedString AddProxy { get; }
    public LocalizedString EditProxy { get; }
    public LocalizedString DeleteProxy { get; }
    public LocalizedString DuplicateProxy { get; }
    public LocalizedString ClearAll { get; }
    public LocalizedString Save { get; }
    public LocalizedString Cancel { get; }
    public LocalizedString Refresh { get; }
    public LocalizedString Export { get; }
    public LocalizedString Import { get; }
    public LocalizedString ProxyName { get; }
    public LocalizedString ProxyType { get; }
    public LocalizedString LocalIP { get; }
    public LocalizedString LocalPort { get; }
    public LocalizedString RemotePort { get; }
    public LocalizedString CustomDomains { get; }
    public LocalizedString Subdomain { get; }
    public LocalizedString SecretKey { get; }
    public LocalizedString ServerAddress { get; }
    public LocalizedString ServerPort { get; }
    public LocalizedString Token { get; }
    public LocalizedString OIDC { get; }
    public LocalizedString TransportProtocol { get; }
    public LocalizedString EnableTLS { get; }
    public LocalizedString HealthCheck { get; }
    public LocalizedString Plugin { get; }
    public LocalizedString EnableEncryption { get; }
    public LocalizedString EnableCompression { get; }
    public LocalizedString FrpcStatus { get; }
    public LocalizedString FrpcRunning { get; }
    public LocalizedString FrpcNotRunning { get; }
    public LocalizedString ProcessId { get; }
    public LocalizedString Configuration { get; }
    public LocalizedString Visitors { get; }
    public LocalizedString AddVisitor { get; }
    public LocalizedString EditVisitor { get; }
    public LocalizedString DeleteVisitor { get; }
    public LocalizedString VisitorName { get; }
    public LocalizedString VisitorType { get; }
    public LocalizedString ServerName { get; }
    public LocalizedString BindAddr { get; }
    public LocalizedString BindPort { get; }
    public LocalizedString Language { get; }
    public LocalizedString AutoStart { get; }
    public LocalizedString PortableMode { get; }
    public LocalizedString QuickShare { get; }

    private LocalizedString Create(string key)
    {
        var localized = new LocalizedString(_localizationService, key);
        _strings[key] = localized;
        return localized;
    }
}
