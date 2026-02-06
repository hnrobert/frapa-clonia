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
    private List<VisitorConfig> _visitors = new();

    [ObservableProperty]
    private VisitorConfig? _selectedVisitor;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    public IRelayCommand AddVisitorCommand { get; }
    public IRelayCommand EditVisitorCommand { get; }
    public IRelayCommand DeleteVisitorCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public List<string> VisitorTypes { get; } = new()
    {
        "stcp", "xtcp", "sudp"
    };

    public VisitorListViewModel(
        ILogger<VisitorListViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;

        AddVisitorCommand = new RelayCommand(() => AddVisitor());
        EditVisitorCommand = new RelayCommand(() => EditVisitor(), () => SelectedVisitor != null);
        DeleteVisitorCommand = new RelayCommand(async () => await DeleteVisitorAsync(), () => SelectedVisitor != null);
        RefreshCommand = new RelayCommand(async () => await LoadVisitorsAsync());

        _ = Task.Run(LoadVisitorsAsync);
    }

    partial void OnSelectedVisitorChanged(VisitorConfig? value)
    {
        EditVisitorCommand.NotifyCanExecuteChanged();
        DeleteVisitorCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadVisitorsAsync()
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
            Visitors = new List<VisitorConfig>();
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
}
