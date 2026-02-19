using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FrapaClonia.Views;

public partial class MainWindow : Window
{
    private const double MinSidebarWidth = 190;
    private const double MaxSidebarWidth = 280;
    private const double DefaultSidebarWidth = 240;
    private GridSplitter? _gridSplitter;
    private ColumnDefinition? _sidebarColumn;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        // Set up native menu for macOS
        if (OperatingSystem.IsMacOS())
        {
            SetupNativeMenu();
        }
    }

    private void SetupNativeMenu()
    {
        var localizationService = App.Services.GetService<ILocalizationService>();

        // Create the app menu items (these become the app menu dropdown on macOS)
        var appMenu = new NativeMenu();

        // About FrapaClonia
        var aboutItem = new NativeMenuItem((localizationService?.GetString("About") ?? "About") + " FrapaClonia")
        {
            Gesture = KeyGesture.Parse("Cmd+Shift+A")
        };
        aboutItem.Click += OnAboutClick;
        appMenu.Items.Add(aboutItem);

        appMenu.Items.Add(new NativeMenuItemSeparator());

        // Settings
        var settingsItem = new NativeMenuItem(localizationService?.GetString("Settings") ?? "Settings")
        {
            Gesture = KeyGesture.Parse("Cmd+,")
        };
        settingsItem.Click += OnSettingsClick;
        appMenu.Items.Add(settingsItem);

        appMenu.Items.Add(new NativeMenuItemSeparator());

        // Hide FrapaClonia
        var hideItem = new NativeMenuItem("Hide FrapaClonia")
        {
            Gesture = KeyGesture.Parse("Cmd+H")
        };
        hideItem.Click += (_, _) => Hide();
        appMenu.Items.Add(hideItem);

        // Hide Others
        var hideOthersItem = new NativeMenuItem("Hide Others")
        {
            Gesture = KeyGesture.Parse("Cmd+Alt+H")
        };
        hideOthersItem.Click += (_, _) => Hide();
        appMenu.Items.Add(hideOthersItem);

        // Show All
        var showAllItem = new NativeMenuItem("Show All");
        showAllItem.Click += (_, _) => Show();
        appMenu.Items.Add(showAllItem);

        appMenu.Items.Add(new NativeMenuItemSeparator());

        // Quit FrapaClonia
        var quitItem = new NativeMenuItem((localizationService?.GetString("Quit") ?? "Quit") + " FrapaClonia")
        {
            Gesture = KeyGesture.Parse("Cmd+Q")
        };
        quitItem.Click += OnQuitClick;
        appMenu.Items.Add(quitItem);

        // Set the menu directly - this becomes the app menu on macOS
        NativeMenu.SetMenu(this, appMenu);
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        var localizationService = App.Services.GetService<ILocalizationService>();
        var aboutViewModel = new AboutViewModel(localizationService);
        var aboutDialog = new AboutView(aboutViewModel);
        aboutDialog.ShowDialog(this);
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.NavigateToSettingsCommand.Execute(null);
        }
    }

    private void OnQuitClick(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Find the main layout grid (first child of the outer grid)
        if (Content is not Grid { Children.Count: > 0 } outerGrid || outerGrid.Children[0] is not Grid mainGrid) return;
        _sidebarColumn = mainGrid.ColumnDefinitions[0];
        _gridSplitter = mainGrid.Children[1] as GridSplitter;

        // Initialize sidebar width to default
        _sidebarColumn?.Width = new GridLength(DefaultSidebarWidth);

        if (_gridSplitter != null)
        {
            _gridSplitter.DragDelta += OnGridSplitterDragDelta;
        }
    }

    private void OnGridSplitterDragDelta(object? sender, VectorEventArgs e)
    {
        if (_sidebarColumn == null) return;

        var currentWidth = _sidebarColumn.Width.Value;
        var newWidth = currentWidth + e.Vector.X;

        newWidth = newWidth switch
        {
            // Clamp the width
            < MinSidebarWidth => MinSidebarWidth,
            > MaxSidebarWidth => MaxSidebarWidth,
            _ => newWidth
        };

        _sidebarColumn.Width = new GridLength(newWidth);
    }
}
