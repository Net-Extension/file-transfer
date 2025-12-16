using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Base interface for protocol-specific transfer providers.
/// Each provider implements upload/download for a specific protocol (HTTP, FTP, SFTP, etc.).
/// </summary>
public interface ITransferProvider
{
    /// <summary>
    /// Gets the protocol scheme this provider handles (http, https, ftp, sftp, scp, ws, wss, custom).
    /// </summary>
    string Scheme { get; }

    /// <summary>
    /// Uploads a file using this provider's protocol.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer result.</returns>
    Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file using this provider's protocol.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer result.</returns>
    Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the capabilities of this provider.
    /// </summary>
    /// <returns>Transfer capabilities.</returns>
    TransferCapabilities GetCapabilities();

    /// <summary>
    /// Validates that the request is compatible with this provider.
    /// </summary>
    /// <param name="request">The transfer request to validate.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(TransferRequest request, CancellationToken cancellationToken = default);
}
