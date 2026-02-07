using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for visitor list management
/// </summary>
public partial class VisitorListViewModel : ObservableObject
{
    private readonly ILogger<VisitorListViewModel> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;

    [ObservableProperty]
    private List<VisitorConfig> _visitors = [];

    [ObservableProperty]
    private VisitorConfig? _selectedVisitor;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _searchQuery;

    [ObservableProperty]
    private int _selectedTypeFilter;

    [ObservableProperty]
    private int _activeCount;

    public IRelayCommand AddVisitorCommand { get; }
    private IRelayCommand EditVisitorCommand { get; }
    private IRelayCommand DeleteVisitorCommand { get; }
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public IRelayCommand DuplicateVisitorCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public List<string> VisitorTypes { get; } = ["stcp", "xtcp", "sudp"];

    public VisitorListViewModel(
        ILogger<VisitorListViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;

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
                _logger.LogError(e, "Error deleting visitor");
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
                _logger.LogError(e, "Error deleting visitor");
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
                _logger.LogError(e, "Error loading visitors");
            }
        });

        _ = Task.Run(LoadVisitorsAsync);
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

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                Visitors = config.Visitors;

                _logger.LogInformation("Loaded {Count} visitors", Visitors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading visitors");
            Visitors = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddVisitor()
    {
        _logger.LogInformation("Add visitor clicked");
        // TODO: Navigate to visitor editor with new visitor
    }

    private void EditVisitor()
    {
        if (SelectedVisitor == null) return;
        _logger.LogInformation("Edit visitor: {VisitorName}", SelectedVisitor.Name);
        // TODO: Navigate to visitor editor with selected visitor
    }

    private async Task DeleteVisitorAsync()
    {
        if (SelectedVisitor == null) return;

        _logger.LogInformation("Delete visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                config.Visitors.RemoveAll(v => v.Name == SelectedVisitor.Name);
                await _configurationService.SaveConfigurationAsync(configPath, config);

                await LoadVisitorsAsync();
                SelectedVisitor = null;

                _logger.LogInformation("Visitor deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting visitor");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DuplicateVisitorAsync()
    {
        if (SelectedVisitor == null) return;

        _logger.LogInformation("Duplicate visitor: {VisitorName}", SelectedVisitor.Name);

        try
        {
            IsSaving = true;

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

                _logger.LogInformation("Visitor duplicated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating visitor");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
