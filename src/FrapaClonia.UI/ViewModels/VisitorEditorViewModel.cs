using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using FrapaClonia.UI.Services;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.UI.ViewModels;

/// <summary>
/// View model for editing a single visitor configuration
/// </summary>
public partial class VisitorEditorViewModel : ObservableObject
{
    private readonly ILogger<VisitorEditorViewModel>? _logger;
    private readonly IPresetService? _presetService;
    private readonly IValidationService? _validationService;
    private readonly ToastService? _toastService;
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

    // Default constructor for design-time support
    public VisitorEditorViewModel() : this(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<VisitorEditorViewModel>.Instance,
        null!,
        null!,
        null!)
    {
    }

    public VisitorEditorViewModel(
        ILogger<VisitorEditorViewModel> logger,
        IPresetService presetService,
        IValidationService validationService,
        ToastService? toastService,
        VisitorConfig? visitorToEdit = null)
    {
        _logger = logger;
        _presetService = presetService;
        _validationService = validationService;
        _toastService = toastService;
        _originalVisitor = visitorToEdit;

        SaveCommand = new RelayCommand(async void () =>
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error saving visitor");
            }
        }, () => !IsSaving);
        CancelCommand = new RelayCommand(() => _logger?.LogInformation("Cancel edit"));

        if (visitorToEdit != null)
        {
            LoadFromVisitor(visitorToEdit);
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnVisitorNameChanged(string value)
    {
        _ = ValidateAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnVisitorTypeChanged(string value)
    {
        _ = ValidateAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnServerNameChanged(string value)
    {
        _ = ValidateAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSecretKeyChanged(string value)
    {
        _ = ValidateAsync();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnBindPortChanged(int value)
    {
        _ = ValidateAsync();
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
        var validation = _validationService?.ValidateVisitor(visitor) ?? new ValidationResult();
        IsValid = validation.IsValid;
        ValidationError = validation.Errors.FirstOrDefault();
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
                _toastService?.Error("Validation Failed", ValidationError ?? "Please check the form for errors");
                return;
            }

            if (_presetService?.CurrentPreset == null)
            {
                _toastService?.Error("Error", "No active preset");
                return;
            }

            var visitor = CreateVisitorConfig();

            // Remove original visitor if editing
            if (_originalVisitor != null)
            {
                _presetService.CurrentPreset.Configuration.Visitors.RemoveAll(v => v.Name == _originalVisitor.Name);
            }

            _presetService.CurrentPreset.Configuration.Visitors.Add(visitor);

            await _presetService.SaveCurrentPresetAsync();

            _logger?.LogInformation("Visitor saved: {VisitorName}", visitor.Name);
            _toastService?.Success("Saved", $"Visitor '{visitor.Name}' saved successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving visitor");
            ValidationError = "Failed to save visitor";
            _toastService?.Error("Save Failed", $"Could not save visitor: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
