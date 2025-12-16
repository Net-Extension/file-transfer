using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Provides credentials for authentication during file transfers.
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// Gets credentials for the specified URI and protocol.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="scheme">The protocol scheme.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The credentials or null if none are available.</returns>
    Task<TransferCredentials?> GetCredentialsAsync(TransferUri uri, string scheme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if credentials are available for the specified URI.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="scheme">The protocol scheme.</param>
    /// <returns>True if credentials are available.</returns>
    Task<bool> HasCredentialsAsync(TransferUri uri, string scheme);
}
