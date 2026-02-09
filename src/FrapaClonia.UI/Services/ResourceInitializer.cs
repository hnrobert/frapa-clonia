using Avalonia.Controls;
using Avalonia.Threading;

namespace FrapaClonia.UI.Services;

/// <summary>
/// Initializes application resources with localized strings
/// </summary>
public static class ResourceInitializer
{
    public static void AddLocalizedResources(IResourceDictionary resources, LocalizedResources localizedResources)
    {
        // Dashboard & Navigation
        AddResource(resources, "Localized_Dashboard", localizedResources.Dashboard);
        AddResource(resources, "Localized_ServerConfig", localizedResources.ServerConfig);
        AddResource(resources, "Localized_ProxyManagement", localizedResources.ProxyManagement);
        AddResource(resources, "Localized_Deployment", localizedResources.Deployment);
        AddResource(resources, "Localized_Settings", localizedResources.Settings);
        AddResource(resources, "Localized_Logs", localizedResources.Logs);
        AddResource(resources, "Localized_Visitors", localizedResources.Visitors);
        AddResource(resources, "Localized_About", localizedResources.About);

        // Frpc Control
        AddResource(resources, "Localized_StartFrpc", localizedResources.StartFrpc);
        AddResource(resources, "Localized_StopFrpc", localizedResources.StopFrpc);
        AddResource(resources, "Localized_RestartFrpc", localizedResources.RestartFrpc);

        // Common Actions
        AddResource(resources, "Localized_AddProxy", localizedResources.AddProxy);
        AddResource(resources, "Localized_EditProxy", localizedResources.EditProxy);
        AddResource(resources, "Localized_DeleteProxy", localizedResources.DeleteProxy);
        AddResource(resources, "Localized_DuplicateProxy", localizedResources.DuplicateProxy);
        AddResource(resources, "Localized_ClearAll", localizedResources.ClearAll);
        AddResource(resources, "Localized_Save", localizedResources.Save);
        AddResource(resources, "Localized_Cancel", localizedResources.Cancel);
        AddResource(resources, "Localized_Refresh", localizedResources.Refresh);
        AddResource(resources, "Localized_Export", localizedResources.Export);
        AddResource(resources, "Localized_Import", localizedResources.Import);

        // Proxy Properties
        AddResource(resources, "Localized_ProxyName", localizedResources.ProxyName);
        AddResource(resources, "Localized_ProxyType", localizedResources.ProxyType);
        AddResource(resources, "Localized_LocalIP", localizedResources.LocalIP);
        AddResource(resources, "Localized_LocalPort", localizedResources.LocalPort);
        AddResource(resources, "Localized_RemotePort", localizedResources.RemotePort);
        AddResource(resources, "Localized_CustomDomains", localizedResources.CustomDomains);
        AddResource(resources, "Localized_Subdomain", localizedResources.Subdomain);
        AddResource(resources, "Localized_SecretKey", localizedResources.SecretKey);

        // Server Configuration
        AddResource(resources, "Localized_ServerAddress", localizedResources.ServerAddress);
        AddResource(resources, "Localized_ServerPort", localizedResources.ServerPort);
        AddResource(resources, "Localized_ServerName", localizedResources.ServerName);
        AddResource(resources, "Localized_Token", localizedResources.Token);
        AddResource(resources, "Localized_OIDC", localizedResources.OIDC);
        AddResource(resources, "Localized_TransportProtocol", localizedResources.TransportProtocol);
        AddResource(resources, "Localized_EnableTLS", localizedResources.EnableTLS);
        AddResource(resources, "Localized_HealthCheck", localizedResources.HealthCheck);
        AddResource(resources, "Localized_Plugin", localizedResources.Plugin);
        AddResource(resources, "Localized_EnableEncryption", localizedResources.EnableEncryption);
        AddResource(resources, "Localized_EnableCompression", localizedResources.EnableCompression);

        // Status
        AddResource(resources, "Localized_FrpcStatus", localizedResources.FrpcStatus);
        AddResource(resources, "Localized_FrpcRunning", localizedResources.FrpcRunning);
        AddResource(resources, "Localized_FrpcNotRunning", localizedResources.FrpcNotRunning);
        AddResource(resources, "Localized_ProcessId", localizedResources.ProcessId);
        AddResource(resources, "Localized_Configuration", localizedResources.Configuration);

        // Visitor Management
        AddResource(resources, "Localized_AddVisitor", localizedResources.AddVisitor);
        AddResource(resources, "Localized_EditVisitor", localizedResources.EditVisitor);
        AddResource(resources, "Localized_DeleteVisitor", localizedResources.DeleteVisitor);
        AddResource(resources, "Localized_VisitorName", localizedResources.VisitorName);
        AddResource(resources, "Localized_VisitorType", localizedResources.VisitorType);
        AddResource(resources, "Localized_BindAddr", localizedResources.BindAddr);
        AddResource(resources, "Localized_BindPort", localizedResources.BindPort);

        // Settings
        AddResource(resources, "Localized_Language", localizedResources.Language);
        AddResource(resources, "Localized_AutoStart", localizedResources.AutoStart);
        AddResource(resources, "Localized_PortableMode", localizedResources.PortableMode);
        AddResource(resources, "Localized_QuickAccess", localizedResources.QuickAccess);

        // Dashboard
        AddResource(resources, "Localized_Overview", localizedResources.Overview);
        AddResource(resources, "Localized_ActiveProxies", localizedResources.ActiveProxies);
        AddResource(resources, "Localized_RecentActivity", localizedResources.RecentActivity);
        AddResource(resources, "Localized_Configure", localizedResources.Configure);
        AddResource(resources, "Localized_Manage", localizedResources.Manage);
        AddResource(resources, "Localized_Setup", localizedResources.Setup);
        AddResource(resources, "Localized_View", localizedResources.View);

        // Server Config Details
        AddResource(resources, "Localized_ConnectionSettings", localizedResources.ConnectionSettings);
        AddResource(resources, "Localized_TheAddressOfTheFrpsServer", localizedResources.TheAddressOfTheFrpsServer);
        AddResource(resources, "Localized_DefaultPort", localizedResources.DefaultPort);
        AddResource(resources, "Localized_OptionalUserName", localizedResources.OptionalUserName);
        AddResource(resources, "Localized_OptionalUserNameToAvoidProxyNameConflicts",
            localizedResources.OptionalUserNameToAvoidProxyNameConflicts);
        AddResource(resources, "Localized_Authentication", localizedResources.Authentication);
        AddResource(resources, "Localized_Method", localizedResources.Method);
        AddResource(resources, "Localized_ClientID", localizedResources.ClientId);
        AddResource(resources, "Localized_ClientSecret", localizedResources.ClientSecret);
        AddResource(resources, "Localized_TokenEndpoint", localizedResources.TokenEndpoint);
        AddResource(resources, "Localized_Audience", localizedResources.Audience);
        AddResource(resources, "Localized_Scope", localizedResources.Scope);
        AddResource(resources, "Localized_EnterYourAuthenticationToken",
            localizedResources.EnterYourAuthenticationToken);
        AddResource(resources, "Localized_OptionalAudience", localizedResources.OptionalAudience);
        AddResource(resources, "Localized_EGOpenidProfile", localizedResources.EgOpenidProfile);
        AddResource(resources, "Localized_TransportSettings", localizedResources.TransportSettings);
        AddResource(resources, "Localized_DialTimeout", localizedResources.DialTimeout);
        AddResource(resources, "Localized_Seconds", localizedResources.Seconds);
        AddResource(resources, "Localized_TCPMux", localizedResources.TcpMux);
        AddResource(resources, "Localized_HeartbeatInterval", localizedResources.HeartbeatInterval);
        AddResource(resources, "Localized_HeartbeatTimeout", localizedResources.HeartbeatTimeout);
        AddResource(resources, "Localized_EncryptCommunicationWithTheFrpsServer",
            localizedResources.EncryptCommunicationWithTheFrpsServer);
        AddResource(resources, "Localized_LoggingSection", localizedResources.LoggingSection);
        AddResource(resources, "Localized_MaxDays", localizedResources.MaxDays);
        AddResource(resources, "Localized_LogTo", localizedResources.LogTo);
        AddResource(resources, "Localized_EGConsolePathToLogfile", localizedResources.EgConsolePathToLogfile);
        AddResource(resources, "Localized_StatusSection", localizedResources.StatusSection);
        AddResource(resources, "Localized_ConfigurationIsValid", localizedResources.ConfigurationIsValid);
        AddResource(resources, "Localized_ResetToDefaults", localizedResources.ResetToDefaults);
        AddResource(resources, "Localized_SaveConfiguration", localizedResources.SaveConfiguration);

        // Deployment
        AddResource(resources, "Localized_DeploymentMode", localizedResources.DeploymentMode);
        AddResource(resources, "Localized_ChooseHowYouWantToDeployAndRunFrpc",
            localizedResources.ChooseHowYouWantToDeployAndRunFrpc);
        AddResource(resources, "Localized_DockerDeployment", localizedResources.DockerDeployment);
        AddResource(resources, "Localized_DockerStatus", localizedResources.DockerStatus);
        AddResource(resources, "Localized_CheckDocker", localizedResources.CheckDocker);
        AddResource(resources, "Localized_ContainerName", localizedResources.ContainerName);
        AddResource(resources, "Localized_DockerImage", localizedResources.DockerImage);
        AddResource(resources, "Localized_DockerComposePath", localizedResources.DockerComposePath);
        AddResource(resources, "Localized_GenerateDockerComposeYml", localizedResources.GenerateDockerComposeYml);
        AddResource(resources, "Localized_StartContainer", localizedResources.StartContainer);
        AddResource(resources, "Localized_StopContainer", localizedResources.StopContainer);
        AddResource(resources, "Localized_NativeDeployment", localizedResources.NativeDeployment);
        AddResource(resources, "Localized_DeploymentStatus", localizedResources.DeploymentStatus);
        AddResource(resources, "Localized_CheckDeployment", localizedResources.CheckDeployment);
        AddResource(resources, "Localized_BinaryPath", localizedResources.BinaryPath);
        AddResource(resources, "Localized_DownloadFrpc", localizedResources.DownloadFrpc);
        AddResource(resources, "Localized_DeployNative", localizedResources.DeployNative);
        AddResource(resources, "Localized_DeploymentInformation", localizedResources.DeploymentInformation);
        AddResource(resources, "Localized_DockerDeploymentRequiresDocker",
            localizedResources.DockerDeploymentRequiresDocker);
        AddResource(resources, "Localized_NativeDeploymentDownloadsAndExtracts",
            localizedResources.NativeDeploymentDownloadsAndExtracts);
        AddResource(resources, "Localized_BothMethodsUse", localizedResources.BothMethodsUse);

        // Proxy List
        AddResource(resources, "Localized_ConfigureAndManageYourFrpcProxyConfigurations",
            localizedResources.ConfigureAndManageYourFrpcProxyConfigurations);
        AddResource(resources, "Localized_Loading", localizedResources.Loading);
        AddResource(resources, "Localized_ProxiesCount", localizedResources.ProxiesCount);
        AddResource(resources, "Localized_SearchByName", localizedResources.SearchByName);
        AddResource(resources, "Localized_AllTypes", localizedResources.AllTypes);
        AddResource(resources, "Localized_NoProxiesConfigured", localizedResources.NoProxiesConfigured);
        AddResource(resources, "Localized_CreateYourFirstProxyConfiguration",
            localizedResources.CreateYourFirstProxyConfiguration);
        AddResource(resources, "Localized_CreateProxy", localizedResources.CreateProxy);
        AddResource(resources, "Localized_Local", localizedResources.Local);
        AddResource(resources, "Localized_RemotePortLabel", localizedResources.RemotePortLabel);
        AddResource(resources, "Localized_EditProxyToolTip", localizedResources.EditProxyToolTip);
        AddResource(resources, "Localized_DuplicateProxyToolTip", localizedResources.DuplicateProxyToolTip);
        AddResource(resources, "Localized_DeleteProxyToolTip", localizedResources.DeleteProxyToolTip);

        // Proxy Editor
        AddResource(resources, "Localized_ProxyConfiguration", localizedResources.ProxyConfiguration);
        AddResource(resources, "Localized_ConfigureYourProxySettingsBelow",
            localizedResources.ConfigureYourProxySettingsBelow);
        AddResource(resources, "Localized_RequiredFieldsAreMarked", localizedResources.RequiredFieldsAreMarked);
        AddResource(resources, "Localized_BasicSettings", localizedResources.BasicSettings);
        AddResource(resources, "Localized_LocalSettings", localizedResources.LocalSettings);
        AddResource(resources, "Localized_TheIPOfTheLocalService", localizedResources.TheIPOfTheLocalService);
        AddResource(resources, "Localized_DomainSettings", localizedResources.DomainSettings);
        AddResource(resources, "Localized_CommaSeparatedList", localizedResources.CommaSeparatedList);
        AddResource(resources, "Localized_LeaveEmptyIfUsingCustomDomains",
            localizedResources.LeaveEmptyIfUsingCustomDomains);
        AddResource(resources, "Localized_EG_www_example_com", localizedResources.EgWwwExampleCom);
        AddResource(resources, "Localized_HTTPAuthentication", localizedResources.HTTPAuthentication);
        AddResource(resources, "Localized_Username", localizedResources.Username);
        AddResource(resources, "Localized_Password", localizedResources.Password);
        AddResource(resources, "Localized_SecureTunnelSettings", localizedResources.SecureTunnelSettings);
        AddResource(resources, "Localized_MustMatchTheVisitorsSecretKey",
            localizedResources.MustMatchTheVisitorsSecretKey);
        AddResource(resources, "Localized_AllowedUsers", localizedResources.AllowedUsers);
        AddResource(resources, "Localized_CommaSeparatedListOfAllowedUsers",
            localizedResources.CommaSeparatedListOfAllowedUsers);
        AddResource(resources, "Localized_LeaveEmptyToAllowAllUsers", localizedResources.LeaveEmptyToAllowAllUsers);
        AddResource(resources, "Localized_TCPMultiplexerSettings", localizedResources.TcpMultiplexerSettings);
        AddResource(resources, "Localized_MultiplexerName", localizedResources.MultiplexerName);
        AddResource(resources, "Localized_NameOfTheMultiplexer", localizedResources.NameOfTheMultiplexer);
        AddResource(resources, "Localized_TheMultiplexerMustBeConfiguredOnTheServer",
            localizedResources.TheMultiplexerMustBeConfiguredOnTheServer);
        AddResource(resources, "Localized_TransportOptions", localizedResources.TransportOptions);
        AddResource(resources, "Localized_EncryptTheConnectionBetweenClientAndServer",
            localizedResources.EncryptTheConnectionBetweenClientAndServer);
        AddResource(resources, "Localized_CompressDataToReduceBandwidthUsage",
            localizedResources.CompressDataToReduceBandwidthUsage);
        AddResource(resources, "Localized_BandwidthLimit", localizedResources.BandwidthLimit);
        AddResource(resources, "Localized_EG_1MB_256KB", localizedResources.Eg1Mb256Kb);
        AddResource(resources, "Localized_LimitBandwidthForThisProxy", localizedResources.LimitBandwidthForThisProxy);
        AddResource(resources, "Localized_EnableHealthCheck", localizedResources.EnableHealthCheck);
        AddResource(resources, "Localized_MonitorTheHealthOfTheLocalService",
            localizedResources.MonitorTheHealthOfTheLocalService);
        AddResource(resources, "Localized_TypeLabel", localizedResources.TypeLabel);
        AddResource(resources, "Localized_TimeoutSeconds", localizedResources.TimeoutSeconds);
        AddResource(resources, "Localized_MaxFailed", localizedResources.MaxFailed);
        AddResource(resources, "Localized_IntervalSeconds", localizedResources.IntervalSeconds);
        AddResource(resources, "Localized_PathLabel", localizedResources.PathLabel);
        AddResource(resources, "Localized_HeadersLabel", localizedResources.HeadersLabel);
        AddResource(resources, "Localized_HeaderNameValue", localizedResources.HeaderNameValue);
        AddResource(resources, "Localized_OneHeaderPerLine", localizedResources.OneHeaderPerLine);
        AddResource(resources, "Localized_PluginConfiguration", localizedResources.PluginConfiguration);
        AddResource(resources, "Localized_PluginTypeLabel", localizedResources.PluginTypeLabel);
        AddResource(resources, "Localized_HTTPProxyURL", localizedResources.HTTPProxyUrl);
        AddResource(resources, "Localized_SOCKS5URL", localizedResources.Socks5Url);
        AddResource(resources, "Localized_StaticFilePluginSettings", localizedResources.StaticFilePluginSettings);
        AddResource(resources, "Localized_LocalPath", localizedResources.LocalPath);
        AddResource(resources, "Localized_URLPrefix", localizedResources.UrlPrefix);
        AddResource(resources, "Localized_HTTPSToHTTPPluginSettings", localizedResources.HttpsToHTTPPluginSettings);
        AddResource(resources, "Localized_LocalAddress", localizedResources.LocalAddress);
        AddResource(resources, "Localized_CRTPath", localizedResources.CrtPath);
        AddResource(resources, "Localized_KeyPath", localizedResources.KeyPath);
        AddResource(resources, "Localized_HTTPToHTTPSPluginSettings", localizedResources.HTTPToHttpsPluginSettings);
        AddResource(resources, "Localized_ConfigurationError", localizedResources.ConfigurationError);

        // Visitor List
        AddResource(resources, "Localized_VisitorManagement", localizedResources.VisitorManagement);
        AddResource(resources, "Localized_ConfigureAndManageSecureTunnelVisitors",
            localizedResources.ConfigureAndManageSecureTunnelVisitors);
        AddResource(resources, "Localized_SearchVisitors", localizedResources.SearchVisitors);
        AddResource(resources, "Localized_Active", localizedResources.Active);
        AddResource(resources, "Localized_Total", localizedResources.Total);
        AddResource(resources, "Localized_LoadingVisitors", localizedResources.LoadingVisitors);
        AddResource(resources, "Localized_NoVisitorsConfigured", localizedResources.NoVisitorsConfigured);
        AddResource(resources, "Localized_VisitorsAllowYouToConnect", localizedResources.VisitorsAllowYouToConnect);
        AddResource(resources, "Localized_CreateYourFirstVisitor", localizedResources.CreateYourFirstVisitor);
        AddResource(resources, "Localized_VisitorConfiguration", localizedResources.VisitorConfiguration);
        AddResource(resources, "Localized_ConfigureAVisitorToConnect", localizedResources.ConfigureAVisitorToConnect);
        AddResource(resources, "Localized_SecuritySettings", localizedResources.SecuritySettings);
        AddResource(resources, "Localized_Required", localizedResources.Required);
        AddResource(resources, "Localized_MustMatchTheProxySecretKey", localizedResources.MustMatchTheProxySecretKey);
        AddResource(resources, "Localized_BindSettings", localizedResources.BindSettings);
        AddResource(resources, "Localized_LocalPortToListenOn", localizedResources.LocalPortToListenOn);
        AddResource(resources, "Localized_SpecificIPToBindTo", localizedResources.SpecificIPToBindTo);
        AddResource(resources, "Localized_OptionalLeaveEmptyToBindToAll",
            localizedResources.OptionalLeaveEmptyToBindToAll);

        // Logs
        AddResource(resources, "Localized_ViewRealtimeLogsFromFrpc", localizedResources.ViewRealtimeLogsFromFrpc);
        AddResource(resources, "Localized_Entries", localizedResources.Entries);
        AddResource(resources, "Localized_LogLevelLabel", localizedResources.LogLevelLabel);
        AddResource(resources, "Localized_AllLevels", localizedResources.AllLevels);
        AddResource(resources, "Localized_AutoScroll", localizedResources.AutoScroll);
        AddResource(resources, "Localized_ReloadLogsFromBuffer", localizedResources.ReloadLogsFromBuffer);
        AddResource(resources, "Localized_ClearAllLogs", localizedResources.ClearAllLogs);
        AddResource(resources, "Localized_ExportLogsToFile", localizedResources.ExportLogsToFile);
        AddResource(resources, "Localized_NoLogsAvailable", localizedResources.NoLogsAvailable);
        AddResource(resources, "Localized_LogsWillAppearHere", localizedResources.LogsWillAppearHere);

        // Settings
        AddResource(resources, "Localized_LanguageAndRegion", localizedResources.LanguageAndRegion);
        AddResource(resources, "Localized_InterfaceLanguage", localizedResources.InterfaceLanguage);
        AddResource(resources, "Localized_ChangesWillTakeEffectImmediately",
            localizedResources.ChangesWillTakeEffectImmediately);
        AddResource(resources, "Localized_SystemIntegration", localizedResources.SystemIntegration);
        AddResource(resources, "Localized_StartOnSystemBoot", localizedResources.StartOnSystemBoot);
        AddResource(resources, "Localized_AutomaticallyLaunchFrapaClonia",
            localizedResources.AutomaticallyLaunchFrapaClonia);
        AddResource(resources, "Localized_Appearance", localizedResources.Appearance);
        AddResource(resources, "Localized_Theme", localizedResources.Theme);
        AddResource(resources, "Localized_ChooseYourPreferredColorTheme",
            localizedResources.ChooseYourPreferredColorTheme);
        AddResource(resources, "Localized_Light", localizedResources.Light);
        AddResource(resources, "Localized_Dark", localizedResources.Dark);
        AddResource(resources, "Localized_SystemDefault", localizedResources.SystemDefault);
        AddResource(resources, "Localized_ApplicationMode", localizedResources.ApplicationMode);
        AddResource(resources, "Localized_ConfigurationStoredInAppDir", localizedResources.ConfigurationStoredInAppDir);
        AddResource(resources, "Localized_ConfigurationLocation", localizedResources.ConfigurationLocation);
        AddResource(resources, "Localized_VersionInfo", localizedResources.VersionInfo);
        AddResource(resources, "Localized_BuildInfo", localizedResources.BuildInfo);
        AddResource(resources, "Localized_NETRuntimeInfo", localizedResources.NetRuntimeInfo);
        AddResource(resources, "Localized_CrossPlatformFrpcVisualClient",
            localizedResources.CrossPlatformFrpcVisualClient);
        AddResource(resources, "Localized_SaveSettings", localizedResources.SaveSettings);
    }

    private static void AddResource(IResourceDictionary resources, string key, LocalizedString localizedString)
    {
        resources[key] = localizedString.Value;

        // Subscribe to property changes and immediately update the resource dictionary
        localizedString.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LocalizedString.Value)) return;

            var value = localizedString.Value;
            if (string.IsNullOrEmpty(value))
            {
                value = key;
            }

            // Update immediately on UI thread
            if (Dispatcher.UIThread.CheckAccess())
            {
                resources[key] = value;
            }
            else
            {
                Dispatcher.UIThread.Post(() => resources[key] = value);
            }
        };
    }
}
