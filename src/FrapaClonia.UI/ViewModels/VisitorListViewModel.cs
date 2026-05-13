using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Shared.Interfaces;
using FrapaClonia.UI.Services;
using FrapaClonia.UI.Utils;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using FrapaClonia.Shared.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for visitor list management
/// </summary>
public partial class VisitorListViewModel : ObservableObject
{
    private readonly ILogger<VisitorListViewModel>? _logger;
    private readonly IValidationService? _validationService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ToastService? _toastService;
    private readonly IPresetService? _presetService;

    [ObservableProperty] private List<VisitorConfig> _visitors = [];

    [ObservableProperty] private VisitorConfig? _selectedVisitor;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private string? _searchQuery;

    [ObservableProperty] private int _selectedTypeFilter;

    [ObservableProperty] private int _activeCount;

    public IRelayCommand AddVisitorCommand { get; }
    public IRelayCommand EditVisitorCommand { get; }
    public IRelayCommand DeleteVisitorCommand { get; }
    public IRelayCommand DuplicateVisitorCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    // ReSharper disable once UnusedMember.Global
    public List<string> VisitorTypes { get; } = ["stcp", "xtcp", "sudp"];

    // Default constructor for design-time support
    public VisitorListViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<VisitorListViewModel>.Instance,
        null!,
        null!,
        null!,
        null!)
    {
    }

    public VisitorListViewModel(
        ILogger<VisitorListViewModel> logger,
        IValidationService validationService,
        IServiceProvider serviceProvider,
        ToastService? toastService,
        IPresetService presetService)
    {
        _logger = logger;
        _validationService = validationService;
        _serviceProvider = serviceProvider;
        _toastService = toastService;
        _presetService = presetService;

        AddVisitorCommand = new RelayCommand(AddVisitor);
        EditVisitorCommand = new RelayCommand(EditVisitor, () => SelectedVisitor != null);
        DeleteVisitorCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DeleteVisitorAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error deleting visitor");
            }
        }, () => SelectedVisitor != null);
        DuplicateVisitorCommand = new RelayCommand(async void () =>
        {
            try
            {
                await DuplicateVisitorAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error deleting visitor");
            }
        }, () => SelectedVisitor != null);
        RefreshCommand = new RelayCommand(async void () =>
        {
            try
            {
                await LoadVisitorsAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error loading visitors");
            }
        });

        // Subscribe to preset changes
        if (_presetService != null)
        {
            _presetService.CurrentPresetChanged += OnCurrentPresetChanged;
        }

        // Note: Loading is initiated by the View's OnLoaded event
    }

    private void OnCurrentPresetChanged(object? sender, PresetChangedEventArgs e)
    {
        // Reload visitors when preset changes
        _ = LoadVisitorsAsync();
    }

    public void Initialize()
    {
        _ = LoadVisitorsAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedVisitorChanged(VisitorConfig? value)
    {
        EditVisitorCommand.NotifyCanExecuteChanged();
        DeleteVisitorCommand.NotifyCanExecuteChanged();
    }

    private Task LoadVisitorsAsync()
    {
        try
        {
            IsLoading = true;
            _logger?.LogDebug("LoadVisitorsAsync: Starting, IsLoading={IsLoading}", IsLoading);

            if (_presetService?.CurrentPreset != null)
            {
                Visitors = _presetService.CurrentPreset.Configuration.Visitors;
                _logger?.LogDebug("Loaded {Count} visitors", Visitors.Count);
            }
            else
            {
                Visitors = [];
                _logger?.LogDebug("No current preset, setting Visitors to empty list");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading visitors");
            Visitors = [];
        }
        finally
        {
            IsLoading = false;
            _logger?.LogDebug("LoadVisitorsAsync: Completed, IsLoading={IsLoading}", IsLoading);
        }

        return Task.CompletedTask;
    }

    private async void AddVisitor()
    {
        try
        {
            _logger?.LogDebug("Add visitor clicked");

            if (WindowReuse.ActivateExisting<VisitorEditorView>() != null)
            {
                return;
            }

            if (_presetService?.CurrentPreset == null)
            {
                _toastService?.Error("Error", "No active preset");
                return;
            }

            // Create new visitor and show editor dialog
            var newVisitor = new VisitorConfig();
            if (_serviceProvider == null) return;
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<VisitorEditorViewModel>>();
            if (_validationService == null) return;
            var viewModel = new VisitorEditorViewModel(editorLogger, _presetService, _validationService, _toastService, newVisitor);

            var editorWindow = new VisitorEditorView
            {
                DataContext = viewModel
            };

            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop)
                return;
            if (desktop.MainWindow == null) return;
            var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                // User clicked Save - refresh the list
                _ = LoadVisitorsAsync();
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error adding visitor");
        }
    }

    private async void EditVisitor()
    {
        try
        {
            if (SelectedVisitor == null) return;
            _logger?.LogDebug("Edit visitor: {VisitorName}", SelectedVisitor.Name);

            if (WindowReuse.ActivateExisting<VisitorEditorView>() != null)
            {
                return;
            }

            if (_presetService?.CurrentPreset == null)
            {
                _toastService?.Error("Error", "No active preset");
                return;
            }

            // Clone the visitor manually to avoid modifying the original until saved
            var visitorClone = CloneVisitor(SelectedVisitor);

            if (_serviceProvider == null) return;
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<VisitorEditorViewModel>>();

            if (_validationService == null) return;
            var viewModel = new VisitorEditorViewModel(editorLogger, _presetService, _validationService, _toastService, visitorClone);

            var editorWindow = new VisitorEditorView
            {
                DataContext = viewModel
            };

            if (Avalonia.Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
                desktop) return;
            if (desktop.MainWindow == null) return;
            var result = await editorWindow.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                // User clicked Save - refresh the list
                _ = LoadVisitorsAsync();
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error editing visitor");
        }
    }

    private static VisitorConfig CloneVisitor(VisitorConfig source)
    {
        return new VisitorConfig
        {
            Name = source.Name,
            Type = source.Type,
            ServerName = source.ServerName,
            SecretKey = source.SecretKey,
            BindAddr = source.BindAddr,
            BindPort = source.BindPort,
            BindIp = source.BindIp,
            Transport = source.Transport,
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value)
        };
    }

    private async Task DeleteVisitorAsync()
    {
        if (SelectedVisitor == null) return;
        if (_presetService?.CurrentPreset == null) return;

        _logger?.LogDebug("Delete visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

            _presetService.CurrentPreset.Configuration.Visitors.RemoveAll(v => v.Name == SelectedVisitor.Name);
            await _presetService.SaveCurrentPresetAsync();

            await LoadVisitorsAsync();
            SelectedVisitor = null;

            _logger?.LogDebug("Visitor deleted successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting visitor");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DuplicateVisitorAsync()
    {
        if (SelectedVisitor == null) return;
        if (_presetService?.CurrentPreset == null) return;

        _logger?.LogDebug("Duplicate visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

            var duplicate = CloneVisitor(SelectedVisitor);
            duplicate.Name = $"{SelectedVisitor.Name} (Copy)";
            duplicate.BindPort = SelectedVisitor.BindPort + 1;

            _presetService.CurrentPreset.Configuration.Visitors.Add(duplicate);
            await _presetService.SaveCurrentPresetAsync();

            await LoadVisitorsAsync();

            _logger?.LogDebug("Visitor duplicated successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error duplicating visitor");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
