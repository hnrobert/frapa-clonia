using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.UI.Models;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using FrapaClonia.Domain.Models;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// Main window view model
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILogger<MainWindowViewModel>? _logger;
    private readonly NavigationService? _navigation;
    private readonly IPresetService? _presetService;

    private const double MinSidebarWidth = 190;
    private const double MaxSidebarWidth = 280;

    [ObservableProperty] private Control? _currentView;
    [ObservableProperty] private string _currentPage = "dashboard";
    [ObservableProperty] private double _sidebarWidth = MinSidebarWidth;

    // Preset-related properties
    [ObservableProperty] private ObservableCollection<PresetItem> _presetItems = [];
    private PresetItem? _selectedPresetItem;

    /// <summary>
    /// The currently selected preset item in the dropdown
    /// </summary>
    public PresetItem? SelectedPresetItem
    {
        get => _selectedPresetItem;
        set
        {
            if (SetProperty(ref _selectedPresetItem, value) && value != null)
            {
                _ = OnSelectedPresetItemChangedAsync(value);
            }
        }
    }

    /// <summary>
    /// The toast notification service
    /// </summary>
    public ToastService? ToastService { get; private set; }

    /// <summary>
    /// Collection of active toast notifications for binding
    /// </summary>
    public ObservableCollection<ToastItem> Toasts => ToastService?.Toasts ?? [];

    /// <summary>
    /// Collection of all presets
    /// </summary>
    public ObservableCollection<ConfigPreset> Presets => _presetService?.Presets ?? [];

    /// <summary>
    /// The currently active preset
    /// </summary>
    public ConfigPreset? CurrentPreset => _presetService?.CurrentPreset;

    // Active state properties for navigation
    public bool IsDashboardActive => CurrentPage == "dashboard";
    public bool IsServerActive => CurrentPage == "server";
    public bool IsProxiesActive => CurrentPage == "proxies";
    public bool IsVisitorsActive => CurrentPage == "visitors";
    public bool IsDeploymentActive => CurrentPage == "deployment";
    public bool IsLogsActive => CurrentPage == "logs";
    public bool IsSettingsActive => CurrentPage == "settings";

    partial void OnCurrentPageChanged(string value)
    {
        _ = value; // Discard parameter without warning
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsServerActive));
        OnPropertyChanged(nameof(IsProxiesActive));
        OnPropertyChanged(nameof(IsVisitorsActive));
        OnPropertyChanged(nameof(IsDeploymentActive));
        OnPropertyChanged(nameof(IsLogsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    partial void OnSidebarWidthChanged(double value)
    {
        SidebarWidth = value switch
        {
            // Clamp width to min/max bounds
            < MinSidebarWidth => MinSidebarWidth,
            > MaxSidebarWidth => MaxSidebarWidth,
            _ => value
        };
    }

    public static string Version
    {
        get
        {
            var informationalVersion = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrEmpty(informationalVersion))
                return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

#if DEBUG
            // Debug: Show full version with build metadata
            return informationalVersion;
#else
            // Release: Strip build metadata (everything after '+')
            var plusIndex = informationalVersion.IndexOf('+');
            return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
#endif
        }
    }

    public static string Copyright => "© 2025 Robert He";

    public IRelayCommand NavigateToDashboardCommand { get; }
    public IRelayCommand NavigateToServerConfigCommand { get; }
    public IRelayCommand NavigateToProxiesCommand { get; }
    public IRelayCommand NavigateToVisitorsCommand { get; }
    public IRelayCommand NavigateToDeploymentCommand { get; }
    public IRelayCommand NavigateToLogsCommand { get; }
    public IRelayCommand NavigateToSettingsCommand { get; }

    // Default constructor for design-time support
    public MainWindowViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<MainWindowViewModel>.Instance,
        null!,
        null!,
        null!)
    {
    }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        NavigationService navigation,
        ToastService? toastService,
        IPresetService? presetService)
    {
        _logger = logger;
        _navigation = navigation;
        ToastService = toastService;
        _presetService = presetService;

        NavigateToDashboardCommand = new RelayCommand(() => Navigate("dashboard"));
        NavigateToServerConfigCommand = new RelayCommand(() => Navigate("server"));
        NavigateToProxiesCommand = new RelayCommand(() => Navigate("proxies"));
        NavigateToVisitorsCommand = new RelayCommand(() => Navigate("visitors"));
        NavigateToDeploymentCommand = new RelayCommand(() => Navigate("deployment"));
        NavigateToLogsCommand = new RelayCommand(() => Navigate("logs"));
        NavigateToSettingsCommand = new RelayCommand(() => Navigate("settings"));

        // Subscribe to navigation changes
        if (_navigation != null)
        {
            _navigation.PageChanged += (_, _) =>
            {
                CurrentView = _navigation.CurrentView;
                CurrentPage = _navigation.CurrentPage;
            };
        }

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
            _presetService.Presets.CollectionChanged += OnPresetsCollectionChanged;
        }

        // Initialize with dashboard
        Navigate("dashboard");

        _logger.LogInformation("MainWindowViewModel initialized");
    }

    /// <summary>
    /// Initialize the preset service (call after construction)
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_presetService != null)
        {
            await _presetService.InitializeAsync();
            UpdatePresetItems();

            // Select current preset
            if (_presetService.CurrentPreset != null)
            {
                _selectedPresetItem = PresetItems.FirstOrDefault(p => p.Id == _presetService.CurrentPreset.Id);
                OnPropertyChanged(nameof(SelectedPresetItem));
            }
        }
    }

    /// <summary>
    /// Initialize the preset selector UI after the preset service is ready
    /// Called from App.axaml.cs after async initialization completes
    /// </summary>
    public void InitializePresetSelector()
    {
        UpdatePresetItems();

        // Select current preset
        if (_presetService?.CurrentPreset != null)
        {
            _selectedPresetItem = PresetItems.FirstOrDefault(p => p.Id == _presetService.CurrentPreset.Id);
            OnPropertyChanged(nameof(SelectedPresetItem));
        }

        _logger?.LogInformation("Preset selector initialized with {Count} presets", PresetItems.Count);
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        UpdatePresetItems();
        OnPropertyChanged(nameof(CurrentPreset));

        // Update selection
        if (e.CurrentPreset != null)
        {
            _selectedPresetItem = PresetItems.FirstOrDefault(p => p.Id == e.CurrentPreset.Id);
            OnPropertyChanged(nameof(SelectedPresetItem));
        }

        _logger?.LogInformation("Current preset changed to: {Name}", e.CurrentPreset?.Name);
    }

    private void OnPresetsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update the preset items when presets are added or removed
        UpdatePresetItems();

        // Keep the current preset selected
        if (_presetService?.CurrentPreset != null)
        {
            _selectedPresetItem = PresetItems.FirstOrDefault(p => p.Id == _presetService.CurrentPreset.Id);
            OnPropertyChanged(nameof(SelectedPresetItem));
        }

        _logger?.LogInformation("Presets collection changed: {Action}", e.Action);
    }

    private void UpdatePresetItems()
    {
        PresetItems.Clear();

        // Add all presets
        foreach (var preset in Presets)
        {
            PresetItems.Add(new PresetItem(preset));
        }

        // Add "+ New Preset..." option
        PresetItems.Add(new PresetItem("+ New Preset...", true));
    }

    private async Task OnSelectedPresetItemChangedAsync(PresetItem item)
    {
        if (item.IsNewPresetOption)
        {
            // Create new preset
            try
            {
                var newPreset = await _presetService!.CreatePresetAsync($"Preset {Presets.Count + 1}");
                await _presetService.SwitchPresetAsync(newPreset.Id);

                ToastService?.Success("Preset Created", $"Created new preset: {newPreset.Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create new preset");
                ToastService?.Error("Error", "Failed to create new preset");

                // Reset selection to current preset
                ResetPresetSelection();
            }
        }
        else if (item.Id != null && item.Id != _presetService?.CurrentPreset?.Id)
        {
            // Switch to selected preset
            try
            {
                await _presetService!.SwitchPresetAsync(item.Id.Value);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to switch preset");
                ToastService?.Error("Error", "Failed to switch preset");

                // Reset selection to current preset
                ResetPresetSelection();
            }
        }
    }

    private void ResetPresetSelection()
    {
        _selectedPresetItem = PresetItems.FirstOrDefault(p => p.Id == _presetService?.CurrentPreset?.Id);
        OnPropertyChanged(nameof(SelectedPresetItem));
    }

    private void Navigate(string page)
    {
        _navigation?.NavigateTo(page);
        // _logger?.LogInformation("Navigated to: {Page}", page);
    }
}
