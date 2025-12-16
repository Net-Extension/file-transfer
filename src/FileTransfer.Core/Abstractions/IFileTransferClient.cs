using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Primary interface for file transfer operations.
/// Provides a unified API for uploading and downloading files across different protocols.
/// </summary>
public interface IFileTransferClient
{
    /// <summary>
    /// Uploads a file to the specified destination.
    /// </summary>
    /// <param name="request">The transfer request containing source, destination, and options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the transfer operation.</returns>
    Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the specified source.
    /// </summary>
    /// <param name="request">The transfer request containing source, destination, and options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the transfer operation.</returns>
    Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the capabilities of this transfer client.
    /// </summary>
    /// <returns>Transfer capabilities indicating supported features.</returns>
    TransferCapabilities GetCapabilities();
}
