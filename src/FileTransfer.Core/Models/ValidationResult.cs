namespace FileTransfer.Core.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; init; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>(errors)
        };
    }

    /// <summary>
    /// Creates a failed validation result with a single error message.
    /// </summary>
    public static ValidationResult Failure(string errorMessage)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new ValidationError
                {
                    Message = errorMessage,
                    Code = TransferErrorCode.ValidationFailed
                }
            }
        };
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public TransferErrorCode Code { get; init; } = TransferErrorCode.ValidationFailed;

    /// <summary>
    /// Gets the property name that failed validation.
    /// </summary>
    public string? PropertyName { get; init; }
}
