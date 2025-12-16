namespace FileTransfer.Core.Models;

/// <summary>
/// Configuration options for file transfer operations.
/// </summary>
public sealed record TransferOptions
{
    /// <summary>
    /// Gets the chunk size in bytes for chunked transfers (default: 4MB).
    /// </summary>
    public int ChunkSize { get; init; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets the maximum number of retry attempts (default: 3).
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the timeout for the entire transfer operation.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the timeout for individual operations (connect, read, write).
    /// </summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the degree of parallelism for concurrent chunk transfers (default: 1 for sequential).
    /// </summary>
    public int DegreeOfParallelism { get; init; } = 1;

    /// <summary>
    /// Gets whether to enable compression.
    /// </summary>
    public bool EnableCompression { get; init; }

    /// <summary>
    /// Gets the compression algorithm to use.
    /// </summary>
    public CompressionAlgorithm CompressionAlgorithm { get; init; } = CompressionAlgorithm.Gzip;

    /// <summary>
    /// Gets whether to enable encryption.
    /// </summary>
    public bool EnableEncryption { get; init; }

    /// <summary>
    /// Gets the encryption algorithm to use.
    /// </summary>
    public EncryptionAlgorithm EncryptionAlgorithm { get; init; } = EncryptionAlgorithm.AesGcm;

    /// <summary>
    /// Gets the encryption key (required if EnableEncryption is true).
    /// </summary>
    public byte[]? EncryptionKey { get; init; }

    /// <summary>
    /// Gets whether to compute and verify checksums.
    /// </summary>
    public bool VerifyChecksum { get; init; } = true;

    /// <summary>
    /// Gets the checksum algorithm to use.
    /// </summary>
    public ChecksumAlgorithm ChecksumAlgorithm { get; init; } = ChecksumAlgorithm.Sha256;

    /// <summary>
    /// Gets whether to enable per-chunk checksums.
    /// </summary>
    public bool EnableChunkChecksums { get; init; }

    /// <summary>
    /// Gets whether to enable resume support for interrupted transfers.
    /// </summary>
    public bool EnableResume { get; init; }

    /// <summary>
    /// Gets the file path for storing resume state.
    /// </summary>
    public string? ResumeStatePath { get; init; }

    /// <summary>
    /// Gets whether to overwrite existing files.
    /// </summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>
    /// Gets the maximum file size allowed (null for unlimited).
    /// </summary>
    public long? MaxFileSize { get; init; }

    /// <summary>
    /// Gets the allowed file extensions (null for all extensions).
    /// </summary>
    public HashSet<string>? AllowedExtensions { get; init; }

    /// <summary>
    /// Gets the allowed hosts for transfers (null for all hosts).
    /// </summary>
    public HashSet<string>? AllowedHosts { get; init; }

    /// <summary>
    /// Gets the denied hosts for transfers.
    /// </summary>
    public HashSet<string>? DeniedHosts { get; init; }

    /// <summary>
    /// Gets whether to prevent path traversal attacks.
    /// </summary>
    public bool PreventPathTraversal { get; init; } = true;

    /// <summary>
    /// Gets the minimum TLS version for HTTPS transfers.
    /// </summary>
    public TlsVersion MinimumTlsVersion { get; init; } = TlsVersion.Tls12;

    /// <summary>
    /// Gets whether to count records for line-based files.
    /// </summary>
    public bool CountRecords { get; init; }

    /// <summary>
    /// Gets whether to buffer the entire file in memory (false for streaming).
    /// </summary>
    public bool BufferInMemory { get; init; }

    /// <summary>
    /// Gets the buffer size for streaming operations (default: 81920 bytes).
    /// </summary>
    public int BufferSize { get; init; } = 81920;
}
