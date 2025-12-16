using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Factory;

/// <summary>
/// Factory for creating transfer providers based on URI scheme.
/// </summary>
public sealed class TransferProviderFactory : ITransferProviderFactory
{
    private readonly Dictionary<string, ITransferProvider> _providers;
    private readonly ILogger<TransferProviderFactory> _logger;

    public TransferProviderFactory(
        IEnumerable<ITransferProvider> providers,
        ILogger<TransferProviderFactory> logger)
    {
        _logger = logger;
        _providers = providers.ToDictionary(p => p.Scheme.ToLowerInvariant(), p => p);

        _logger.LogInformation("TransferProviderFactory initialized with {ProviderCount} providers: {Schemes}",
            _providers.Count, string.Join(", ", _providers.Keys));
    }

    public ITransferProvider GetProvider(string scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            throw new ArgumentException("Scheme cannot be null or empty.", nameof(scheme));
        }

        var normalizedScheme = scheme.ToLowerInvariant();

        if (_providers.TryGetValue(normalizedScheme, out var provider))
        {
            _logger.LogDebug("Found provider for scheme: {Scheme}", scheme);
            return provider;
        }

        _logger.LogError("No provider found for scheme: {Scheme}. Available schemes: {AvailableSchemes}",
            scheme, string.Join(", ", _providers.Keys));

        throw new NotSupportedException(
            $"No transfer provider is registered for scheme '{scheme}'. " +
            $"Available schemes: {string.Join(", ", _providers.Keys)}");
    }

    public bool TryGetProvider(string scheme, out ITransferProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            provider = null;
            return false;
        }

        var normalizedScheme = scheme.ToLowerInvariant();
        return _providers.TryGetValue(normalizedScheme, out provider);
    }

    public IEnumerable<ITransferProvider> GetAllProviders()
    {
        return _providers.Values;
    }

    public bool SupportsScheme(string scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            return false;
        }

        return _providers.ContainsKey(scheme.ToLowerInvariant());
    }
}
