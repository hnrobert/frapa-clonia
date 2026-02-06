using FrapaClonia.Core.Interfaces;
using FrapaClonia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for validating frp configurations
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateConfiguration(FrpClientConfig configuration)
    {
        // TODO: Implement in Phase 2
        return ValidationResult.Success;
    }

    public ValidationResult ValidateProxy(ProxyConfig proxy)
    {
        // TODO: Implement in Phase 2
        return ValidationResult.Success;
    }

    public ValidationResult ValidateServerConnection(ClientCommonConfig serverConfig)
    {
        // TODO: Implement in Phase 2
        return ValidationResult.Success;
    }
}
