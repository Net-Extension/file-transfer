using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using Microsoft.Extensions.Options;

namespace FileTransfer.Security;

/// <summary>
/// Default credential provider that retrieves credentials from configuration.
/// </summary>
public sealed class DefaultCredentialProvider : ICredentialProvider
{
    private readonly Dictionary<string, TransferCredentials> _credentials = new();

    public DefaultCredentialProvider(IOptions<CredentialOptions>? options = null)
    {
        if (options?.Value?.Credentials != null)
        {
            foreach (var kvp in options.Value.Credentials)
            {
                _credentials[kvp.Key.ToLowerInvariant()] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Registers credentials for a specific host.
    /// </summary>
    public void RegisterCredentials(string host, TransferCredentials credentials)
    {
        _credentials[host.ToLowerInvariant()] = credentials;
    }

    public Task<TransferCredentials?> GetCredentialsAsync(TransferUri uri, string scheme, CancellationToken cancellationToken = default)
    {
        var key = uri.Host.ToLowerInvariant();

        if (_credentials.TryGetValue(key, out var credentials))
        {
            return Task.FromResult<TransferCredentials?>(credentials);
        }

        // Try with scheme + host combination
        var schemeHostKey = $"{scheme}://{uri.Host}".ToLowerInvariant();
        if (_credentials.TryGetValue(schemeHostKey, out credentials))
        {
            return Task.FromResult<TransferCredentials?>(credentials);
        }

        return Task.FromResult<TransferCredentials?>(null);
    }

    public Task<bool> HasCredentialsAsync(TransferUri uri, string scheme)
    {
        var key = uri.Host.ToLowerInvariant();
        var schemeHostKey = $"{scheme}://{uri.Host}".ToLowerInvariant();

        return Task.FromResult(_credentials.ContainsKey(key) || _credentials.ContainsKey(schemeHostKey));
    }
}

/// <summary>
/// Options for configuring credentials.
/// </summary>
public sealed class CredentialOptions
{
    public Dictionary<string, TransferCredentials> Credentials { get; set; } = new();
}
