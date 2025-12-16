namespace FileTransfer.Core.Models;

/// <summary>
/// Describes the capabilities of a transfer provider.
/// </summary>
public sealed record TransferCapabilities
{
    /// <summary>
    /// Gets whether the provider supports chunked transfers.
    /// </summary>
    public bool SupportsChunking { get; init; }

    /// <summary>
    /// Gets whether the provider supports resume.
    /// </summary>
    public bool SupportsResume { get; init; }

    /// <summary>
    /// Gets whether the provider supports checksum verification.
    /// </summary>
    public bool SupportsChecksum { get; init; }

    /// <summary>
    /// Gets whether the provider supports compression.
    /// </summary>
    public bool SupportsCompression { get; init; }

    /// <summary>
    /// Gets whether the provider supports encryption.
    /// </summary>
    public bool SupportsEncryption { get; init; }

    /// <summary>
    /// Gets whether the provider supports parallel transfers.
    /// </summary>
    public bool SupportsParallelism { get; init; }

    /// <summary>
    /// Gets the maximum file size supported (null for unlimited).
    /// </summary>
    public long? MaxFileSize { get; init; }

    /// <summary>
    /// Gets the supported checksum algorithms.
    /// </summary>
    public HashSet<ChecksumAlgorithm> SupportedChecksumAlgorithms { get; init; } = new();

    /// <summary>
    /// Gets the supported compression algorithms.
    /// </summary>
    public HashSet<CompressionAlgorithm> SupportedCompressionAlgorithms { get; init; } = new();

    /// <summary>
    /// Gets the supported encryption algorithms.
    /// </summary>
    public HashSet<EncryptionAlgorithm> SupportedEncryptionAlgorithms { get; init; } = new();

    /// <summary>
    /// Gets the supported credential types.
    /// </summary>
    public HashSet<CredentialType> SupportedCredentialTypes { get; init; } = new();
}
