using FrapaClonia.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace FrapaClonia.UI.Services;

/// <summary>
/// Provides localized string resources as dynamic properties for XAML binding
/// </summary>
public class LocalizedResources : ObservableObject
{
    private readonly ILocalizationService _localizationService;

    // ReSharper disable once UnusedMember.Local
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
        ExportAll = Create("ExportAll");
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

        // Dashboard
        QuickAccess = Create("QuickAccess");
        Overview = Create("Overview");
        ActiveProxies = Create("ActiveProxies");
        RecentActivity = Create("RecentActivity");
        Configure = Create("Configure");
        Manage = Create("Manage");
        Setup = Create("Setup");
        View = Create("View");

        // Server Config
        ConnectionSettings = Create("ConnectionSettings");
        TheAddressOfTheFrpsServer = Create("TheAddressOfTheFrpsServer");
        DefaultPort = Create("DefaultPort");
        OptionalUserName = Create("OptionalUserName");
        OptionalUserNameToAvoidProxyNameConflicts = Create("OptionalUserNameToAvoidProxyNameConflicts");
        Authentication = Create("Authentication");
        Method = Create("Method");
        ClientId = Create("ClientID");
        ClientSecret = Create("ClientSecret");
        TokenEndpoint = Create("TokenEndpoint");
        Audience = Create("Audience");
        Scope = Create("Scope");
        EnterYourAuthenticationToken = Create("EnterYourAuthenticationToken");
        OptionalAudience = Create("OptionalAudience");
        EgOpenidProfile = Create("EGOpenidProfile");
        TransportSettings = Create("TransportSettings");
        DialTimeout = Create("DialTimeout");
        Seconds = Create("Seconds");
        TcpMux = Create("TCPMux");
        HeartbeatInterval = Create("HeartbeatInterval");
        HeartbeatTimeout = Create("HeartbeatTimeout");
        EncryptCommunicationWithTheFrpsServer = Create("EncryptCommunicationWithTheFrpsServer");
        LoggingSection = Create("LoggingSection");
        MaxDays = Create("MaxDays");
        LogTo = Create("LogTo");
        EgConsolePathToLogfile = Create("EGConsolePathToLogfile");
        StatusSection = Create("StatusSection");
        ConfigurationIsValid = Create("ConfigurationIsValid");
        ResetToDefaults = Create("ResetToDefaults");
        SaveConfiguration = Create("SaveConfiguration");

        // Deployment
        DeploymentMode = Create("DeploymentMode");
        ChooseHowYouWantToDeployAndRunFrpc = Create("ChooseHowYouWantToDeployAndRunFrpc");
        DockerDeployment = Create("DockerDeployment");
        DockerStatus = Create("DockerStatus");
        CheckDocker = Create("CheckDocker");
        ContainerName = Create("ContainerName");
        DockerImage = Create("DockerImage");
        DockerComposePath = Create("DockerComposePath");
        GenerateDockerComposeYml = Create("GenerateDockerComposeYml");
        StartContainer = Create("StartContainer");
        StopContainer = Create("StopContainer");
        NativeDeployment = Create("NativeDeployment");
        DeploymentStatus = Create("DeploymentStatus");
        CheckDeployment = Create("CheckDeployment");
        BinaryPath = Create("BinaryPath");
        DownloadFrpc = Create("DownloadFrpc");
        DeployNative = Create("DeployNative");
        DeploymentInformation = Create("DeploymentInformation");
        DockerDeploymentRequiresDocker = Create("DockerDeploymentRequiresDocker");
        NativeDeploymentDownloadsAndExtracts = Create("NativeDeploymentDownloadsAndExtracts");
        BothMethodsUse = Create("BothMethodsUse");
        // Enhanced Deployment
        FrpcVersion = Create("FrpcVersion");
        CheckVersions = Create("CheckVersions");
        InstallMethod = Create("InstallMethod");
        SelectFrpcExecutable = Create("SelectFrpcExecutable");
        InstallViaPackageManager = Create("InstallViaPackageManager");
        DownloadFromGitHub = Create("DownloadFromGitHub");
        FrpcBinaryPath = Create("FrpcBinaryPath");
        SelectFrpcBinary = Create("SelectFrpcBinary");
        Browse = Create("Browse");
        PackageManager = Create("PackageManager");
        Install = Create("Install");
        DownloadFrpcFromGitHub = Create("DownloadFrpcFromGitHub");
        DownloadAndDeploy = Create("DownloadAndDeploy");
        ServiceConfiguration = Create("ServiceConfiguration");
        InstallAsSystemService = Create("InstallAsSystemService");
        ServiceScope = Create("ServiceScope");
        UserLevel = Create("UserLevel");
        SystemLevel = Create("SystemLevel");
        StartOnBoot = Create("StartOnBoot");
        ServiceStatus = Create("ServiceStatus");
        InstallService = Create("InstallService");
        UninstallService = Create("UninstallService");
        Start = Create("Start");
        Stop = Create("Stop");
        // New dialog strings
        FrpcConfiguration = Create("FrpcConfiguration");
        FrpcExecutableLocation = Create("FrpcExecutableLocation");
        FrpcExecutablePathDescription = Create("FrpcExecutablePathDescription");
        AutoDetect = Create("AutoDetect");
        AutoDetectFrpcToolTip = Create("AutoDetectFrpcToolTip");
        BrowseToolTip = Create("BrowseToolTip");
        FrpcNotFoundOrInvalid = Create("FrpcNotFoundOrInvalid");
        DetectingFrpc = Create("DetectingFrpc");
        GetFrpc = Create("GetFrpc");
        GetFrpcDescription = Create("GetFrpcDescription");
        UsePackageManager = Create("UsePackageManager");
        DownloadFromWeb = Create("DownloadFromWeb");
        SelectPackageManager = Create("SelectPackageManager");
        RefreshPackageManagerList = Create("RefreshPackageManagerList");
        InstallCommand = Create("InstallCommand");
        PackageManagerNotSupportFrpc = Create("PackageManagerNotSupportFrpc");
        DownloadFromWebDescription = Create("DownloadFromWebDescription");
        OpenReleasesPage = Create("OpenReleasesPage");
        Check = Create("Check");
        CheckFrpcPathToolTip = Create("CheckFrpcPathToolTip");
        ConfigureFrpcToolTip = Create("ConfigureFrpcToolTip");
        CheckingFrpc = Create("CheckingFrpc");
        FrpcNotConfigured = Create("FrpcNotConfigured");
        SystemServiceSettings = Create("SystemServiceSettings");
        DownloadDirect = Create("DownloadDirect");
        DownloadDirectToolTip = Create("DownloadDirectToolTip");
        OpenReleasesPageToolTip = Create("OpenReleasesPageToolTip");
        Downloading = Create("Downloading");

        // Proxy List
        ConfigureAndManageYourFrpcProxyConfigurations = Create("ConfigureAndManageYourFrpcProxyConfigurations");
        Loading = Create("Loading");
        ProxiesCount = Create("ProxiesCount");
        SearchByName = Create("SearchByName");
        AllTypes = Create("AllTypes");
        NoProxiesConfigured = Create("NoProxiesConfigured");
        CreateYourFirstProxyConfiguration = Create("CreateYourFirstProxyConfiguration");
        CreateProxy = Create("CreateProxy");
        Local = Create("Local");
        RemotePortLabel = Create("RemotePortLabel");
        EditProxyToolTip = Create("EditProxyToolTip");
        DuplicateProxyToolTip = Create("DuplicateProxyToolTip");
        DeleteProxyToolTip = Create("DeleteProxyToolTip");

        // Proxy Editor
        EditProxyWindowTitle = Create("EditProxyWindowTitle");
        ProxyConfiguration = Create("ProxyConfiguration");
        ConfigureYourProxySettingsBelow = Create("ConfigureYourProxySettingsBelow");
        RequiredFieldsAreMarked = Create("RequiredFieldsAreMarked");
        BasicSettings = Create("BasicSettings");
        LocalSettings = Create("LocalSettings");
        TheIPOfTheLocalService = Create("TheIPOfTheLocalService");
        DomainSettings = Create("DomainSettings");
        CommaSeparatedList = Create("CommaSeparatedList");
        LeaveEmptyIfUsingCustomDomains = Create("LeaveEmptyIfUsingCustomDomains");
        EgWwwExampleCom = Create("EG_www_example_com");
        HTTPAuthentication = Create("HTTPAuthentication");
        Username = Create("Username");
        Password = Create("Password");
        SecureTunnelSettings = Create("SecureTunnelSettings");
        MustMatchTheVisitorsSecretKey = Create("MustMatchTheVisitorsSecretKey");
        AllowedUsers = Create("AllowedUsers");
        CommaSeparatedListOfAllowedUsers = Create("CommaSeparatedListOfAllowedUsers");
        LeaveEmptyToAllowAllUsers = Create("LeaveEmptyToAllowAllUsers");
        TcpMultiplexerSettings = Create("TCPMultiplexerSettings");
        MultiplexerName = Create("MultiplexerName");
        NameOfTheMultiplexer = Create("NameOfTheMultiplexer");
        TheMultiplexerMustBeConfiguredOnTheServer = Create("TheMultiplexerMustBeConfiguredOnTheServer");
        TransportOptions = Create("TransportOptions");
        EncryptTheConnectionBetweenClientAndServer = Create("EncryptTheConnectionBetweenClientAndServer");
        CompressDataToReduceBandwidthUsage = Create("CompressDataToReduceBandwidthUsage");
        BandwidthLimit = Create("BandwidthLimit");
        Eg1Mb256Kb = Create("EG_1MB_256KB");
        LimitBandwidthForThisProxy = Create("LimitBandwidthForThisProxy");
        EnableHealthCheck = Create("EnableHealthCheck");
        MonitorTheHealthOfTheLocalService = Create("MonitorTheHealthOfTheLocalService");
        TypeLabel = Create("TypeLabel");
        TimeoutSeconds = Create("TimeoutSeconds");
        MaxFailed = Create("MaxFailed");
        IntervalSeconds = Create("IntervalSeconds");
        PathLabel = Create("PathLabel");
        HeadersLabel = Create("HeadersLabel");
        HeaderNameValue = Create("HeaderNameValue");
        OneHeaderPerLine = Create("OneHeaderPerLine");
        PluginConfiguration = Create("PluginConfiguration");
        PluginTypeLabel = Create("PluginTypeLabel");
        HTTPProxyUrl = Create("HTTPProxyURL");
        Socks5Url = Create("SOCKS5URL");
        StaticFilePluginSettings = Create("StaticFilePluginSettings");
        LocalPath = Create("LocalPath");
        UrlPrefix = Create("URLPrefix");
        HttpsToHTTPPluginSettings = Create("HTTPSToHTTPPluginSettings");
        LocalAddress = Create("LocalAddress");
        CrtPath = Create("CRTPath");
        KeyPath = Create("KeyPath");
        HTTPToHttpsPluginSettings = Create("HTTPToHTTPSPluginSettings");
        ConfigurationError = Create("ConfigurationError");

        // Visitor List
        VisitorManagement = Create("VisitorManagement");
        ConfigureAndManageSecureTunnelVisitors = Create("ConfigureAndManageSecureTunnelVisitors");
        SearchVisitors = Create("SearchVisitors");
        Active = Create("Active");
        Total = Create("Total");
        LoadingVisitors = Create("LoadingVisitors");
        NoVisitorsConfigured = Create("NoVisitorsConfigured");
        VisitorsAllowYouToConnect = Create("VisitorsAllowYouToConnect");
        CreateYourFirstVisitor = Create("CreateYourFirstVisitor");
        ServerBindInfo = Create("ServerBindInfo");
        BindIPPortInfo = Create("BindIPPortInfo");
        TransportEnabled = Create("TransportEnabled");

        // Visitor Editor
        EditVisitorWindowTitle = Create("EditVisitorWindowTitle");
        VisitorConfiguration = Create("VisitorConfiguration");
        ConfigureAVisitorToConnect = Create("ConfigureAVisitorToConnect");
        SecuritySettings = Create("SecuritySettings");
        Required = Create("Required");
        MustMatchTheProxySecretKey = Create("MustMatchTheProxySecretKey");
        BindSettings = Create("BindSettings");
        LocalPortToListenOn = Create("LocalPortToListenOn");
        SpecificIPToBindTo = Create("SpecificIPToBindTo");
        OptionalLeaveEmptyToBindToAll = Create("OptionalLeaveEmptyToBindToAll");

        // Logs
        ViewRealtimeLogsFromFrpc = Create("ViewRealtimeLogsFromFrpc");
        Entries = Create("Entries");
        LogLevelLabel = Create("LogLevelLabel");
        AllLevels = Create("AllLevels");
        AutoScroll = Create("AutoScroll");
        ReloadLogsFromBuffer = Create("ReloadLogsFromBuffer");
        ClearAllLogs = Create("ClearAllLogs");
        ExportLogsToFile = Create("ExportLogsToFile");
        NoLogsAvailable = Create("NoLogsAvailable");
        LogsWillAppearHere = Create("LogsWillAppearHere");

        // Settings
        LanguageAndRegion = Create("LanguageAndRegion");
        InterfaceLanguage = Create("InterfaceLanguage");
        ChangesWillTakeEffectImmediately = Create("ChangesWillTakeEffectImmediately");
        SystemIntegration = Create("SystemIntegration");
        StartOnSystemBoot = Create("StartOnSystemBoot");
        AutomaticallyLaunchFrapaClonia = Create("AutomaticallyLaunchFrapaClonia");
        Appearance = Create("Appearance");
        Theme = Create("Theme");
        ChooseYourPreferredColorTheme = Create("ChooseYourPreferredColorTheme");
        Light = Create("Light");
        Dark = Create("Dark");
        SystemDefault = Create("SystemDefault");
        ApplicationMode = Create("ApplicationMode");
        ConfigurationStoredInAppDir = Create("ConfigurationStoredInAppDir");
        ConfigurationLocation = Create("ConfigurationLocation");
        VersionInfo = Create("VersionInfo");
        BuildInfo = Create("BuildInfo");
        NetRuntimeInfo = Create("NETRuntimeInfo");
        CrossPlatformFrpcVisualClient = Create("CrossPlatformFrpcVisualClient");
        SaveSettings = Create("SaveSettings");

        // Preset Management
        ConfigPresets = Create("ConfigPresets");
        PresetManagement = Create("PresetManagement");
        PresetName = Create("PresetName");
        RenamePreset = Create("RenamePreset");
        DeletePreset = Create("DeletePreset");
        DuplicatePreset = Create("DuplicatePreset");
        ExportPreset = Create("ExportPreset");
        ImportPreset = Create("ImportPreset");
        PresetExported = Create("PresetExported");
        PresetImported = Create("PresetImported");
        ExportFailed = Create("ExportFailed");
        ImportFailed = Create("ImportFailed");
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
    public LocalizedString ExportAll { get; }
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

    // Dashboard
    public LocalizedString QuickAccess { get; }
    public LocalizedString Overview { get; }
    public LocalizedString ActiveProxies { get; }
    public LocalizedString RecentActivity { get; }
    public LocalizedString Configure { get; }
    public LocalizedString Manage { get; }
    public LocalizedString Setup { get; }
    public LocalizedString View { get; }

    // Server Config
    public LocalizedString ConnectionSettings { get; }
    public LocalizedString TheAddressOfTheFrpsServer { get; }
    public LocalizedString DefaultPort { get; }
    public LocalizedString OptionalUserName { get; }
    public LocalizedString OptionalUserNameToAvoidProxyNameConflicts { get; }
    public LocalizedString Authentication { get; }
    public LocalizedString Method { get; }
    public LocalizedString ClientId { get; }
    public LocalizedString ClientSecret { get; }
    public LocalizedString TokenEndpoint { get; }
    public LocalizedString Audience { get; }
    public LocalizedString Scope { get; }
    public LocalizedString EnterYourAuthenticationToken { get; }
    public LocalizedString OptionalAudience { get; }
    public LocalizedString EgOpenidProfile { get; }
    public LocalizedString TransportSettings { get; }
    public LocalizedString DialTimeout { get; }
    public LocalizedString Seconds { get; }
    public LocalizedString TcpMux { get; }
    public LocalizedString HeartbeatInterval { get; }
    public LocalizedString HeartbeatTimeout { get; }
    public LocalizedString EncryptCommunicationWithTheFrpsServer { get; }
    public LocalizedString LoggingSection { get; }
    public LocalizedString MaxDays { get; }
    public LocalizedString LogTo { get; }
    public LocalizedString EgConsolePathToLogfile { get; }
    public LocalizedString StatusSection { get; }
    public LocalizedString ConfigurationIsValid { get; }
    public LocalizedString ResetToDefaults { get; }
    public LocalizedString SaveConfiguration { get; }

    // Deployment
    public LocalizedString DeploymentMode { get; }
    public LocalizedString ChooseHowYouWantToDeployAndRunFrpc { get; }
    public LocalizedString DockerDeployment { get; }
    public LocalizedString DockerStatus { get; }
    public LocalizedString CheckDocker { get; }
    public LocalizedString ContainerName { get; }
    public LocalizedString DockerImage { get; }
    public LocalizedString DockerComposePath { get; }
    public LocalizedString GenerateDockerComposeYml { get; }
    public LocalizedString StartContainer { get; }
    public LocalizedString StopContainer { get; }
    public LocalizedString NativeDeployment { get; }
    public LocalizedString DeploymentStatus { get; }
    public LocalizedString CheckDeployment { get; }
    public LocalizedString BinaryPath { get; }
    public LocalizedString DownloadFrpc { get; }
    public LocalizedString DeployNative { get; }
    public LocalizedString DeploymentInformation { get; }
    public LocalizedString DockerDeploymentRequiresDocker { get; }
    public LocalizedString NativeDeploymentDownloadsAndExtracts { get; }
    public LocalizedString BothMethodsUse { get; }
    // Enhanced Deployment
    public LocalizedString FrpcVersion { get; }
    public LocalizedString CheckVersions { get; }
    public LocalizedString InstallMethod { get; }
    public LocalizedString SelectFrpcExecutable { get; }
    public LocalizedString InstallViaPackageManager { get; }
    public LocalizedString DownloadFromGitHub { get; }
    public LocalizedString FrpcBinaryPath { get; }
    public LocalizedString SelectFrpcBinary { get; }
    public LocalizedString Browse { get; }
    public LocalizedString PackageManager { get; }
    public LocalizedString Install { get; }
    public LocalizedString DownloadFrpcFromGitHub { get; }
    public LocalizedString DownloadAndDeploy { get; }
    public LocalizedString ServiceConfiguration { get; }
    public LocalizedString InstallAsSystemService { get; }
    public LocalizedString ServiceScope { get; }
    public LocalizedString UserLevel { get; }
    public LocalizedString SystemLevel { get; }
    public LocalizedString StartOnBoot { get; }
    public LocalizedString ServiceStatus { get; }
    public LocalizedString InstallService { get; }
    public LocalizedString UninstallService { get; }
    public LocalizedString Start { get; }
    public LocalizedString Stop { get; }
    // New dialog strings
    public LocalizedString FrpcConfiguration { get; }
    public LocalizedString FrpcExecutableLocation { get; }
    public LocalizedString FrpcExecutablePathDescription { get; }
    public LocalizedString AutoDetect { get; }
    public LocalizedString AutoDetectFrpcToolTip { get; }
    public LocalizedString BrowseToolTip { get; }
    public LocalizedString FrpcNotFoundOrInvalid { get; }
    public LocalizedString DetectingFrpc { get; }
    public LocalizedString GetFrpc { get; }
    public LocalizedString GetFrpcDescription { get; }
    public LocalizedString UsePackageManager { get; }
    public LocalizedString DownloadFromWeb { get; }
    public LocalizedString SelectPackageManager { get; }
    public LocalizedString RefreshPackageManagerList { get; }
    public LocalizedString InstallCommand { get; }
    public LocalizedString PackageManagerNotSupportFrpc { get; }
    public LocalizedString DownloadFromWebDescription { get; }
    public LocalizedString OpenReleasesPage { get; }
    public LocalizedString Check { get; }
    public LocalizedString CheckFrpcPathToolTip { get; }
    public LocalizedString ConfigureFrpcToolTip { get; }
    public LocalizedString CheckingFrpc { get; }
    public LocalizedString FrpcNotConfigured { get; }
    public LocalizedString SystemServiceSettings { get; }
    public LocalizedString DownloadDirect { get; }
    public LocalizedString DownloadDirectToolTip { get; }
    public LocalizedString OpenReleasesPageToolTip { get; }
    public LocalizedString Downloading { get; }

    // Proxy List
    public LocalizedString ConfigureAndManageYourFrpcProxyConfigurations { get; }
    public LocalizedString Loading { get; }
    public LocalizedString ProxiesCount { get; }
    public LocalizedString SearchByName { get; }
    public LocalizedString AllTypes { get; }
    public LocalizedString NoProxiesConfigured { get; }
    public LocalizedString CreateYourFirstProxyConfiguration { get; }
    public LocalizedString CreateProxy { get; }
    public LocalizedString Local { get; }
    public LocalizedString RemotePortLabel { get; }
    public LocalizedString EditProxyToolTip { get; }
    public LocalizedString DuplicateProxyToolTip { get; }
    public LocalizedString DeleteProxyToolTip { get; }

    // Proxy Editor
    public LocalizedString EditProxyWindowTitle { get; }
    public LocalizedString ProxyConfiguration { get; }
    public LocalizedString ConfigureYourProxySettingsBelow { get; }
    public LocalizedString RequiredFieldsAreMarked { get; }
    public LocalizedString BasicSettings { get; }
    public LocalizedString LocalSettings { get; }
    public LocalizedString TheIPOfTheLocalService { get; }
    public LocalizedString DomainSettings { get; }
    public LocalizedString CommaSeparatedList { get; }
    public LocalizedString LeaveEmptyIfUsingCustomDomains { get; }
    public LocalizedString EgWwwExampleCom { get; }
    public LocalizedString HTTPAuthentication { get; }
    public LocalizedString Username { get; }
    public LocalizedString Password { get; }
    public LocalizedString SecureTunnelSettings { get; }
    public LocalizedString MustMatchTheVisitorsSecretKey { get; }
    public LocalizedString AllowedUsers { get; }
    public LocalizedString CommaSeparatedListOfAllowedUsers { get; }
    public LocalizedString LeaveEmptyToAllowAllUsers { get; }
    public LocalizedString TcpMultiplexerSettings { get; }
    public LocalizedString MultiplexerName { get; }
    public LocalizedString NameOfTheMultiplexer { get; }
    public LocalizedString TheMultiplexerMustBeConfiguredOnTheServer { get; }
    public LocalizedString TransportOptions { get; }
    public LocalizedString EncryptTheConnectionBetweenClientAndServer { get; }
    public LocalizedString CompressDataToReduceBandwidthUsage { get; }
    public LocalizedString BandwidthLimit { get; }
    public LocalizedString Eg1Mb256Kb { get; }
    public LocalizedString LimitBandwidthForThisProxy { get; }
    public LocalizedString EnableHealthCheck { get; }
    public LocalizedString MonitorTheHealthOfTheLocalService { get; }
    public LocalizedString TypeLabel { get; }
    public LocalizedString TimeoutSeconds { get; }
    public LocalizedString MaxFailed { get; }
    public LocalizedString IntervalSeconds { get; }
    public LocalizedString PathLabel { get; }
    public LocalizedString HeadersLabel { get; }
    public LocalizedString HeaderNameValue { get; }
    public LocalizedString OneHeaderPerLine { get; }
    public LocalizedString PluginConfiguration { get; }
    public LocalizedString PluginTypeLabel { get; }
    public LocalizedString HTTPProxyUrl { get; }
    public LocalizedString Socks5Url { get; }
    public LocalizedString StaticFilePluginSettings { get; }
    public LocalizedString LocalPath { get; }
    public LocalizedString UrlPrefix { get; }
    public LocalizedString HttpsToHTTPPluginSettings { get; }
    public LocalizedString LocalAddress { get; }
    public LocalizedString CrtPath { get; }
    public LocalizedString KeyPath { get; }
    public LocalizedString HTTPToHttpsPluginSettings { get; }
    public LocalizedString ConfigurationError { get; }

    // Visitor List
    public LocalizedString VisitorManagement { get; }
    public LocalizedString ConfigureAndManageSecureTunnelVisitors { get; }
    public LocalizedString SearchVisitors { get; }
    public LocalizedString Active { get; }
    public LocalizedString Total { get; }
    public LocalizedString LoadingVisitors { get; }
    public LocalizedString NoVisitorsConfigured { get; }
    public LocalizedString VisitorsAllowYouToConnect { get; }
    public LocalizedString CreateYourFirstVisitor { get; }
    public LocalizedString ServerBindInfo { get; }
    public LocalizedString BindIPPortInfo { get; }
    public LocalizedString TransportEnabled { get; }

    // Visitor Editor
    public LocalizedString EditVisitorWindowTitle { get; }
    public LocalizedString VisitorConfiguration { get; }
    public LocalizedString ConfigureAVisitorToConnect { get; }
    public LocalizedString SecuritySettings { get; }
    public LocalizedString Required { get; }
    public LocalizedString MustMatchTheProxySecretKey { get; }
    public LocalizedString BindSettings { get; }
    public LocalizedString LocalPortToListenOn { get; }
    public LocalizedString SpecificIPToBindTo { get; }
    public LocalizedString OptionalLeaveEmptyToBindToAll { get; }

    // Logs
    public LocalizedString ViewRealtimeLogsFromFrpc { get; }
    public LocalizedString Entries { get; }
    public LocalizedString LogLevelLabel { get; }
    public LocalizedString AllLevels { get; }
    public LocalizedString AutoScroll { get; }
    public LocalizedString ReloadLogsFromBuffer { get; }
    public LocalizedString ClearAllLogs { get; }
    public LocalizedString ExportLogsToFile { get; }
    public LocalizedString NoLogsAvailable { get; }
    public LocalizedString LogsWillAppearHere { get; }

    // Settings
    public LocalizedString LanguageAndRegion { get; }
    public LocalizedString InterfaceLanguage { get; }
    public LocalizedString ChangesWillTakeEffectImmediately { get; }
    public LocalizedString SystemIntegration { get; }
    public LocalizedString StartOnSystemBoot { get; }
    public LocalizedString AutomaticallyLaunchFrapaClonia { get; }
    public LocalizedString Appearance { get; }
    public LocalizedString Theme { get; }
    public LocalizedString ChooseYourPreferredColorTheme { get; }
    public LocalizedString Light { get; }
    public LocalizedString Dark { get; }
    public LocalizedString SystemDefault { get; }
    public LocalizedString ApplicationMode { get; }
    public LocalizedString ConfigurationStoredInAppDir { get; }
    public LocalizedString ConfigurationLocation { get; }
    public LocalizedString VersionInfo { get; }
    public LocalizedString BuildInfo { get; }
    public LocalizedString NetRuntimeInfo { get; }
    public LocalizedString CrossPlatformFrpcVisualClient { get; }
    public LocalizedString SaveSettings { get; }

    // Preset Management
    public LocalizedString ConfigPresets { get; }
    public LocalizedString PresetManagement { get; }
    public LocalizedString PresetName { get; }
    public LocalizedString RenamePreset { get; }
    public LocalizedString DeletePreset { get; }
    public LocalizedString DuplicatePreset { get; }
    public LocalizedString ExportPreset { get; }
    public LocalizedString ImportPreset { get; }
    public LocalizedString PresetExported { get; }
    public LocalizedString PresetImported { get; }
    public LocalizedString ExportFailed { get; }
    public LocalizedString ImportFailed { get; }

    private LocalizedString Create(string key)
    {
        return new LocalizedString(_localizationService, key);
    }
}
