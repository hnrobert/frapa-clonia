using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Infrastructure.Services;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.ViewModels;
using Serilog;
using System.IO;
using System.Text;
using Serilog.Events;

namespace FrapaClonia;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IFrpcProcessService, FrpcProcessService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IPresetService, PresetService>();

        // Infrastructure Services
        services.AddSingleton<IFrpcDownloader, FrpcDownloader>();
        services.AddSingleton<IDockerDeploymentService, DockerDeploymentService>();
        services.AddSingleton<INativeDeploymentService, NativeDeploymentService>();
        services.AddSingleton<ITomlSerializer, TomlSerializer>();
        services.AddSingleton<IProcessManager, ProcessManager>();

        // Services
        services.AddSingleton<NavigationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocalizedResources>();
        services.AddSingleton<ToastService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ServerConfigViewModel>();
        services.AddSingleton<ProxyListViewModel>();
        services.AddSingleton<VisitorListViewModel>();
        services.AddSingleton<DeploymentViewModel>();
        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", standardErrorFromLevel: LogEventLevel.Error)
            .WriteTo.File(Path.Combine(GetAppDataDirectory(), "logs", "frapa-clonia-.log"), rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }

    private static string GetAppDataDirectory()
    {
        return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "FrapaClonia");
    }
}
