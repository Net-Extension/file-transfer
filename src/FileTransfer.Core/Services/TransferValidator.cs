using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;

namespace FileTransfer.Core.Services;

/// <summary>
/// Default implementation of transfer request validator.
/// </summary>
public sealed class TransferValidator : ITransferValidator
{
    public Task<ValidationResult> ValidateAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Validate source
        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors.Add(new ValidationError
            {
                Message = "Source cannot be null or empty.",
                PropertyName = nameof(request.Source),
                Code = TransferErrorCode.ValidationFailed
            });
        }

        // Validate destination
        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            errors.Add(new ValidationError
            {
                Message = "Destination cannot be null or empty.",
                PropertyName = nameof(request.Destination),
                Code = TransferErrorCode.ValidationFailed
            });
        }

        // Validate encryption settings
        if (request.Options.EnableEncryption)
        {
            if (request.Options.EncryptionAlgorithm == EncryptionAlgorithm.None)
            {
                errors.Add(new ValidationError
                {
                    Message = "Encryption is enabled but algorithm is set to None.",
                    PropertyName = nameof(request.Options.EncryptionAlgorithm),
                    Code = TransferErrorCode.EncryptionKeyMissing
                });
            }

            if (request.Options.EncryptionKey == null || request.Options.EncryptionKey.Length == 0)
            {
                errors.Add(new ValidationError
                {
                    Message = "Encryption is enabled but no encryption key provided.",
                    PropertyName = nameof(request.Options.EncryptionKey),
                    Code = TransferErrorCode.EncryptionKeyMissing
                });
            }
        }

        // Validate chunk size
        if (request.Options.ChunkSize <= 0)
        {
            errors.Add(new ValidationError
            {
                Message = "Chunk size must be greater than zero.",
                PropertyName = nameof(request.Options.ChunkSize),
                Code = TransferErrorCode.ValidationFailed
            });
        }

        // Validate timeout
        if (request.Options.Timeout.HasValue && request.Options.Timeout.Value <= TimeSpan.Zero)
        {
            errors.Add(new ValidationError
            {
                Message = "Timeout must be greater than zero.",
                PropertyName = nameof(request.Options.Timeout),
                Code = TransferErrorCode.ValidationFailed
            });
        }

        // Validate path traversal prevention
        if (request.Options.PreventPathTraversal)
        {
            if (request.Destination.Contains("..") || request.Source.Contains(".."))
            {
                errors.Add(new ValidationError
                {
                    Message = "Path traversal detected in source or destination.",
                    Code = TransferErrorCode.PathTraversalDetected
                });
            }
        }

        return Task.FromResult(errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray()));
    }
}
