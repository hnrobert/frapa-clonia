using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using FrapaClonia.UI.ViewModels;
using FrapaClonia.Views;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.MarkupExtensions;
using CommunityToolkit.Mvvm.Input;

namespace FrapaClonia;

public class App : Application
{
    private ServiceProvider? _serviceProvider;
    private MainWindow? _mainWindow;

    // ReSharper disable once UnusedMember.Global
    public static IServiceProvider Services => ((App)Current!)._serviceProvider!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Wire up native menu events for macOS
        if (OperatingSystem.IsMacOS())
        {
            SetupNativeMenuEvents();
        }
    }

    private void SetupNativeMenuEvents()
    {
        // Get the native menu from the Application
        var menu = NativeMenu.GetMenu(this);
        if (menu == null || menu.Items.Count == 0) return;

        // Items are directly in the menu (not nested)
        foreach (var item in menu.Items)
        {
            if (item is not NativeMenuItem menuItem) continue;

            // Use Header to identify menu items
            if (menuItem.Header == null) continue;

            if (menuItem.Header?.Contains("About") == true)
            {
                menuItem.Click += (_, _) => ShowAboutDialog();
            }
            else if (menuItem.Header?.Contains("Settings") == true)
            {
                menuItem.Click += (_, _) => NavigateToSettings();
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>")]
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
            _mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            desktop.MainWindow = _mainWindow;

            // Set up keyboard shortcuts
            SetupKeyboardShortcuts(_mainWindow);

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

    private void SetupKeyboardShortcuts(Window window)
    {
        // Define key bindings
        var keyBindings = new[]
        {
            // Cmd+W - Close window
            (Key.W, KeyModifiers.Meta, "CloseWindow"),
            // Cmd+M - Minimize window
            (Key.M, KeyModifiers.Meta, "MinimizeWindow"),
            // Cmd+, - Settings
            (Key.OemComma, KeyModifiers.Meta, "OpenSettings"),
            // Cmd+Shift+A - About
            (Key.A, KeyModifiers.Meta | KeyModifiers.Shift, "OpenAbout")
        };

        foreach (var (key, modifiers, commandName) in keyBindings)
        {
            var binding = new KeyBinding
            {
                Gesture = new KeyGesture(key, modifiers),
                Command = new RelayCommand(() => ExecuteShortcutCommand(commandName))
            };
            window.KeyBindings.Add(binding);
        }
    }

    private void ExecuteShortcutCommand(string commandName)
    {
        switch (commandName)
        {
            case "CloseWindow":
                _mainWindow?.Close();
                break;
            case "MinimizeWindow":
                _mainWindow?.WindowState = WindowState.Minimized;
                break;
            case "OpenSettings":
                NavigateToSettings();
                break;
            case "OpenAbout":
                ShowAboutDialog();
                break;
        }
    }

    private void NavigateToSettings()
    {
        if (_mainWindow?.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.NavigateToSettingsCommand.Execute(null);
        }
    }

    private void ShowAboutDialog()
    {
        if (_serviceProvider == null) return;

        try
        {
            var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
            var aboutViewModel = new AboutViewModel(localizationService);
            var aboutDialog = new AboutView(aboutViewModel);

            if (_mainWindow != null)
            {
                aboutDialog.ShowDialog(_mainWindow);
            }
            else
            {
                aboutDialog.Show();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to show About dialog: {ex}");
        }
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