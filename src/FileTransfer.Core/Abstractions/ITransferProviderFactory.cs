using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Factory for creating the appropriate transfer provider based on URI scheme.
/// </summary>
public interface ITransferProviderFactory
{
    /// <summary>
    /// Gets a provider that can handle the specified URI scheme.
    /// </summary>
    /// <param name="scheme">The URI scheme (http, https, ftp, sftp, scp, ws, wss, custom).</param>
    /// <returns>The appropriate transfer provider.</returns>
    /// <exception cref="NotSupportedException">Thrown when no provider supports the scheme.</exception>
    ITransferProvider GetProvider(string scheme);

    /// <summary>
    /// Tries to get a provider that can handle the specified URI scheme.
    /// </summary>
    /// <param name="scheme">The URI scheme.</param>
    /// <param name="provider">The provider if found.</param>
    /// <returns>True if a provider was found, false otherwise.</returns>
    bool TryGetProvider(string scheme, out ITransferProvider? provider);

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>Collection of all registered providers.</returns>
    IEnumerable<ITransferProvider> GetAllProviders();

    /// <summary>
    /// Checks if a provider exists for the specified scheme.
    /// </summary>
    /// <param name="scheme">The URI scheme.</param>
    /// <returns>True if a provider exists for the scheme.</returns>
    bool SupportsScheme(string scheme);
}
