using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using FrapaClonia.UI.ViewModels;
using FrapaClonia.Views;
using FrapaClonia.UI.Services;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using FrapaClonia.Core.Interfaces;

namespace FrapaClonia;

public class App : Application
{
    private ServiceProvider? _serviceProvider;
    private static LocalizedResources? _localizedResources;

    // ReSharper disable once UnusedMember.Global
    public static IServiceProvider Services => ((App)Current!)._serviceProvider!;

    // Provides access to localized resources from XAML
    // ReSharper disable once UnusedMember.Global
    public static LocalizedResources LocalizedStrings => _localizedResources ?? throw new InvalidOperationException("LocalizedResources not initialized");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Set up DI container
            var services = new ServiceCollection();
            services.AddApplicationServices();
            _serviceProvider = services.BuildServiceProvider();

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Initialize localized resources
            var localizedResources = _serviceProvider.GetRequiredService<LocalizedResources>();
            _localizedResources = localizedResources;
            ResourceInitializer.AddLocalizedResources(Resources, localizedResources);

            // Resolve MainWindow and its ViewModel from DI container
            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            // Initialize preset service asynchronously after window is created
            var presetService = _serviceProvider.GetRequiredService<IPresetService>();
            Task.Run(async () =>
            {
                try
                {
                    await presetService.InitializeAsync();
                    // Update the ViewModel on the UI thread after initialization
                    Avalonia.Threading.Dispatcher.UIThread.Post(mainWindowViewModel.InitializePresetSelector);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Failed to initialize preset service: {ex}");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    [RequiresUnreferencedCode("Calls Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators")]
    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
