using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for editing a single visitor configuration
/// </summary>
public partial class VisitorEditorViewModel : ObservableObject
{
    private readonly ILogger<VisitorEditorViewModel> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly IValidationService _validationService;
    private readonly VisitorConfig? _originalVisitor;

    [ObservableProperty]
    private string _visitorName = "";

    [ObservableProperty]
    private string _visitorType = "stcp";

    [ObservableProperty]
    private string _serverName = "";

    [ObservableProperty]
    private string _secretKey = "";

    [ObservableProperty]
    private string _bindAddr = "127.0.0.1";

    [ObservableProperty]
    private int _bindPort;

    [ObservableProperty]
    private string? _bindIp;

    [ObservableProperty]
    private bool _isValid = true;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isSaving;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public List<string> VisitorTypes { get; } = ["stcp", "xtcp", "sudp"];

    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationError);

    public VisitorEditorViewModel(
        ILogger<VisitorEditorViewModel> logger,
        IConfigurationService configurationService,
        IValidationService validationService,
        VisitorConfig? visitorToEdit = null)
    {
        _logger = logger;
        _configurationService = configurationService;
        _validationService = validationService;
        _originalVisitor = visitorToEdit;

        SaveCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving visitor");
            }
        }, () => !IsSaving);
        CancelCommand = new RelayCommand(() => _logger.LogInformation("Cancel edit"));

        if (visitorToEdit != null)
        {
            LoadFromVisitor(visitorToEdit);
        }
    }

    private void LoadFromVisitor(VisitorConfig visitor)
    {
        VisitorName = visitor.Name;
        VisitorType = visitor.Type;
        ServerName = visitor.ServerName;
        SecretKey = visitor.SecretKey;
        BindAddr = visitor.BindAddr;
        BindPort = visitor.BindPort;
        BindIp = visitor.BindIp;
        // Note: ClientTransportConfig doesn't have UseEncryption/UseCompression
        // Those are specific to ProxyTransport
    }

    private Task ValidateAsync()
    {
        var visitor = CreateVisitorConfig();
        // Validate required fields
        IsValid = !string.IsNullOrWhiteSpace(VisitorName) &&
                   !string.IsNullOrWhiteSpace(ServerName) &&
                   !string.IsNullOrWhiteSpace(SecretKey) &&
                   BindPort > 0;
        ValidationError = IsValid ? null : "Please fill in all required fields";
        return Task.CompletedTask;
    }

    private VisitorConfig CreateVisitorConfig()
    {
        return new VisitorConfig
        {
            Name = VisitorName,
            Type = VisitorType,
            ServerName = ServerName,
            SecretKey = SecretKey,
            BindAddr = BindAddr,
            BindPort = BindPort,
            BindIp = BindIp
            // Transport is optional for visitors, uses common config transport
        };
    }

    public async Task SaveAsync()
    {
        try
        {
            IsSaving = true;
            ValidationError = null;

            await ValidateAsync();

            if (!IsValid)
            {
                return;
            }

            var configPath = _configurationService.GetDefaultConfigPath();
            var config = await _configurationService.LoadConfigurationAsync(configPath);

            if (config != null)
            {
                var visitor = CreateVisitorConfig();

                // Remove original visitor if editing
                if (_originalVisitor != null)
                {
                    config.Visitors.RemoveAll(v => v.Name == _originalVisitor.Name);
                }

                config.Visitors.Add(visitor);

                await _configurationService.SaveConfigurationAsync(configPath, config);

                _logger.LogInformation("Visitor saved: {VisitorName}", visitor.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving visitor");
            ValidationError = "Failed to save visitor";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
