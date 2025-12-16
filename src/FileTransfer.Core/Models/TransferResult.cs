namespace FileTransfer.Core.Models;

/// <summary>
/// Represents the result of a file transfer operation.
/// </summary>
public sealed record TransferResult
{
    /// <summary>
    /// Gets whether the transfer was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the source location.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the destination location.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Gets the number of bytes transferred.
    /// </summary>
    public required long BytesTransferred { get; init; }

    /// <summary>
    /// Gets the total bytes (file size).
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the duration of the transfer.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the computed checksum (if enabled).
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the checksum algorithm used.
    /// </summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; init; }

    /// <summary>
    /// Gets the number of records transferred (for line-based files).
    /// </summary>
    public long? RecordCount { get; init; }

    /// <summary>
    /// Gets the correlation ID.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the timestamp when the transfer started.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets the timestamp when the transfer completed.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Gets the error if the transfer failed.
    /// </summary>
    public TransferError? Error { get; init; }

    /// <summary>
    /// Gets the protocol/scheme used.
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the average transfer rate in bytes per second.
    /// </summary>
    public double AverageRateBytesPerSecond => Duration.TotalSeconds > 0
        ? BytesTransferred / Duration.TotalSeconds
        : 0;
}
