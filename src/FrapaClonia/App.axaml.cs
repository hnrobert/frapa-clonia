using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using FrapaClonia.UI.ViewModels;
using FrapaClonia.Views;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.MarkupExtensions;

namespace FrapaClonia;

public class App : Application
{
    private ServiceProvider? _serviceProvider;

    // ReSharper disable once UnusedMember.Global
    public static IServiceProvider Services => ((App)Current!)._serviceProvider!;

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

            // Initialize LocalizeExtension with the localization service
            var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
            LocalizeExtension.LocalizationService = localizationService;

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
