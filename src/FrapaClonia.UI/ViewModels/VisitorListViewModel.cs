using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls.ApplicationLifetimes;
using System.Text.Json;
using FrapaClonia.Domain;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for visitor list management
/// </summary>
public partial class VisitorListViewModel : ObservableObject
{
    private readonly ILogger<VisitorListViewModel>? _logger;
    private readonly IConfigurationService? _configurationService;
    private readonly IValidationService? _validationService;
    private readonly IServiceProvider? _serviceProvider;

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
        null!)
    {
    }

    public VisitorListViewModel(
        ILogger<VisitorListViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;
        _serviceProvider = serviceProvider;

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

        // Note: Loading is initiated by the View's OnLoaded event
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

    private async Task LoadVisitorsAsync()
    {
        try
        {
            IsLoading = true;

            if (_configurationService != null)
            {
                var configPath = _configurationService.GetDefaultConfigPath();
                var config = await _configurationService.LoadConfigurationAsync(configPath);

                if (config != null)
                {
                    Visitors = config.Visitors;
                    _logger?.LogInformation("Loaded {Count} visitors", Visitors.Count);
                }
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
        }
    }

    private async void AddVisitor()
    {
        try
        {
            _logger?.LogInformation("Add visitor clicked");

            // Create new visitor and show editor dialog
            var newVisitor = new VisitorConfig();
            if (_serviceProvider == null) return;
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<VisitorEditorViewModel>>();
            if (_configurationService == null) return;
            if (_validationService == null) return;
            var viewModel = new VisitorEditorViewModel(editorLogger, _configurationService, _validationService, newVisitor);

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
            _logger?.LogInformation("Edit visitor: {VisitorName}", SelectedVisitor.Name);

            // Clone the visitor to avoid modifying the original until saved
            var json = JsonSerializer.Serialize(SelectedVisitor, FrpClientConfigContext.Default.VisitorConfig);
            var visitorClone = JsonSerializer.Deserialize(json, FrpClientConfigContext.Default.VisitorConfig);

            if (visitorClone == null) return;
            if (_serviceProvider == null) return;
            var editorLogger = _serviceProvider.GetRequiredService<ILogger<VisitorEditorViewModel>>();

            if (_configurationService == null) return;
            if (_validationService == null) return;
            var viewModel = new VisitorEditorViewModel(editorLogger, _configurationService, _validationService, visitorClone);

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

    private async Task DeleteVisitorAsync()
    {
        if (SelectedVisitor == null) return;

        _logger?.LogInformation("Delete visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

            if (_configurationService != null)
            {
                var configPath = _configurationService.GetDefaultConfigPath();
                var config = await _configurationService.LoadConfigurationAsync(configPath);

                if (config != null)
                {
                    config.Visitors.RemoveAll(v => v.Name == SelectedVisitor.Name);
                    await _configurationService.SaveConfigurationAsync(configPath, config);

                    await LoadVisitorsAsync();
                    SelectedVisitor = null;

                    _logger?.LogInformation("Visitor deleted successfully");
                }
            }
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

        _logger?.LogInformation("Duplicate visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

            if (_configurationService != null)
            {
                var configPath = _configurationService.GetDefaultConfigPath();
                var config = await _configurationService.LoadConfigurationAsync(configPath);

                if (config != null)
                {
                    var duplicate = new VisitorConfig
                    {
                        Name = $"{SelectedVisitor.Name} (Copy)",
                        Type = SelectedVisitor.Type,
                        ServerName = SelectedVisitor.ServerName,
                        SecretKey = SelectedVisitor.SecretKey,
                        BindAddr = SelectedVisitor.BindAddr,
                        BindPort = SelectedVisitor.BindPort + 1,
                        BindIp = SelectedVisitor.BindIp,
                        Transport = SelectedVisitor.Transport
                    };

                    config.Visitors.Add(duplicate);
                    await _configurationService.SaveConfigurationAsync(configPath, config);

                    await LoadVisitorsAsync();

                    _logger?.LogInformation("Visitor duplicated successfully");
                }
            }
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