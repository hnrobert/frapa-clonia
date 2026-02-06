using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Infrastructure.Services;
using Serilog;
using System.IO;

namespace FrapaClonia;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IFrpcProcessService, FrpcProcessService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();

        // Infrastructure Services
        services.AddSingleton<IFrpcDownloader, FrpcDownloader>();
        services.AddSingleton<IDockerDeploymentService, DockerDeploymentService>();
        services.AddSingleton<INativeDeploymentService, NativeDeploymentService>();
        services.AddSingleton<ITomlSerializer, TomlSerializer>();
        services.AddSingleton<IProcessManager, ProcessManager>();

        // Logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(GetAppDataDirectory(), "logs", "frapa-clonia-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        return services;
    }

    private static string GetAppDataDirectory()
    {
        return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "FrapaClonia");
    }
}
