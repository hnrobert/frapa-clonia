using FrapaClonia.Domain.Models;

namespace FrapaClonia.Core.Interfaces;

/// <summary>
/// Service for validating frp configurations
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a complete client configuration
    /// </summary>
    ValidationResult ValidateConfiguration(FrpClientConfig configuration);

    /// <summary>
    /// Validates a proxy configuration
    /// </summary>
    ValidationResult ValidateProxy(ProxyConfig proxy);

    /// <summary>
    /// Validates a server connection configuration
    /// </summary>
    ValidationResult ValidateServerConnection(ClientCommonConfig serverConfig);

    /// <summary>
    /// Validates a visitor configuration
    /// </summary>
    ValidationResult ValidateVisitor(VisitorConfig visitor);
}

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];

    public static ValidationResult Success => new() { IsValid = true };

    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    public ValidationResult WithError(string error)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = Errors.Concat([error]).ToList(),
            Warnings = Warnings
        };
    }

    public ValidationResult WithWarning(string warning)
    {
        return new ValidationResult
        {
            IsValid = IsValid,
            Errors = Errors,
            Warnings = Warnings.Concat([warning]).ToList()
        };
    }
}
