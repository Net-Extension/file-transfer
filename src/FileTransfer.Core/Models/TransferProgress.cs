namespace FileTransfer.Core.Models;

/// <summary>
/// Represents progress information for an ongoing file transfer.
/// </summary>
public sealed record TransferProgress
{
    /// <summary>
    /// Gets the total number of bytes to transfer.
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the number of bytes transferred so far.
    /// </summary>
    public required long BytesTransferred { get; init; }

    /// <summary>
    /// Gets the percentage complete (0-100).
    /// </summary>
    public double PercentComplete => TotalBytes > 0
        ? (BytesTransferred / (double)TotalBytes) * 100
        : 0;

    /// <summary>
    /// Gets the current transfer rate in bytes per second.
    /// </summary>
    public double RateBytesPerSecond { get; init; }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets the current state of the transfer.
    /// </summary>
    public TransferState State { get; init; }

    /// <summary>
    /// Gets the current chunk being transferred (if applicable).
    /// </summary>
    public int? CurrentChunk { get; init; }

    /// <summary>
    /// Gets the total number of chunks (if applicable).
    /// </summary>
    public int? TotalChunks { get; init; }

    /// <summary>
    /// Gets the correlation ID.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets additional status message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a progress report for transfer start.
    /// </summary>
    public static TransferProgress CreateStarted(long totalBytes, string correlationId)
    {
        return new TransferProgress
        {
            TotalBytes = totalBytes,
            BytesTransferred = 0,
            State = TransferState.Starting,
            CorrelationId = correlationId,
            Message = "Transfer starting..."
        };
    }

    /// <summary>
    /// Creates a progress report for an in-progress transfer.
    /// </summary>
    public static TransferProgress CreateInProgress(long totalBytes, long bytesTransferred, double rateBytesPerSecond, string correlationId)
    {
        var remainingBytes = totalBytes - bytesTransferred;
        var eta = rateBytesPerSecond > 0
            ? TimeSpan.FromSeconds(remainingBytes / rateBytesPerSecond)
            : (TimeSpan?)null;

        return new TransferProgress
        {
            TotalBytes = totalBytes,
            BytesTransferred = bytesTransferred,
            RateBytesPerSecond = rateBytesPerSecond,
            EstimatedTimeRemaining = eta,
            State = TransferState.InProgress,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates a progress report for transfer completion.
    /// </summary>
    public static TransferProgress CreateCompleted(long totalBytes, string correlationId)
    {
        return new TransferProgress
        {
            TotalBytes = totalBytes,
            BytesTransferred = totalBytes,
            State = TransferState.Completed,
            CorrelationId = correlationId,
            Message = "Transfer completed successfully"
        };
    }
}
