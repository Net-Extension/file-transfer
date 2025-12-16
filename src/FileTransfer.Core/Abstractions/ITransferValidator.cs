using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Validates transfer requests before execution.
/// </summary>
public interface ITransferValidator
{
    /// <summary>
    /// Validates a transfer request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating if the request is valid and any errors.</returns>
    Task<ValidationResult> ValidateAsync(TransferRequest request, CancellationToken cancellationToken = default);
}
