using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Factory;

/// <summary>
/// Default implementation of IFileTransferClient that uses the provider factory.
/// </summary>
public sealed class FileTransferClient : IFileTransferClient
{
    private readonly ITransferProviderFactory _providerFactory;
    private readonly ILogger<FileTransferClient> _logger;

    public FileTransferClient(
        ITransferProviderFactory providerFactory,
        ILogger<FileTransferClient> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting upload: {Source} -> {Destination} (CorrelationId: {CorrelationId})",
            request.Source, request.Destination, request.CorrelationId);

        var destinationUri = TransferUri.Parse(request.Destination);
        var provider = _providerFactory.GetProvider(destinationUri.Scheme);

        _logger.LogDebug("Using provider {ProviderType} for scheme {Scheme}",
            provider.GetType().Name, destinationUri.Scheme);

        return await provider.UploadAsync(request, cancellationToken);
    }

    public async Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting download: {Source} -> {Destination} (CorrelationId: {CorrelationId})",
            request.Source, request.Destination, request.CorrelationId);

        var sourceUri = TransferUri.Parse(request.Source);
        var provider = _providerFactory.GetProvider(sourceUri.Scheme);

        _logger.LogDebug("Using provider {ProviderType} for scheme {Scheme}",
            provider.GetType().Name, sourceUri.Scheme);

        return await provider.DownloadAsync(request, cancellationToken);
    }

    public TransferCapabilities GetCapabilities()
    {
        // Returns a composite of all provider capabilities
        var allProviders = _providerFactory.GetAllProviders();

        return new TransferCapabilities
        {
            SupportsChunking = allProviders.Any(p => p.GetCapabilities().SupportsChunking),
            SupportsResume = allProviders.Any(p => p.GetCapabilities().SupportsResume),
            SupportsChecksum = allProviders.Any(p => p.GetCapabilities().SupportsChecksum),
            SupportsCompression = allProviders.Any(p => p.GetCapabilities().SupportsCompression),
            SupportsEncryption = allProviders.Any(p => p.GetCapabilities().SupportsEncryption),
            SupportsParallelism = allProviders.Any(p => p.GetCapabilities().SupportsParallelism),
            SupportedChecksumAlgorithms = allProviders
                .SelectMany(p => p.GetCapabilities().SupportedChecksumAlgorithms)
                .ToHashSet(),
            SupportedCompressionAlgorithms = allProviders
                .SelectMany(p => p.GetCapabilities().SupportedCompressionAlgorithms)
                .ToHashSet(),
            SupportedEncryptionAlgorithms = allProviders
                .SelectMany(p => p.GetCapabilities().SupportedEncryptionAlgorithms)
                .ToHashSet(),
            SupportedCredentialTypes = allProviders
                .SelectMany(p => p.GetCapabilities().SupportedCredentialTypes)
                .ToHashSet()
        };
    }
}
