namespace FileTransfer.Core.Models;

/// <summary>
/// Represents a file transfer request with all necessary parameters.
/// </summary>
public sealed record TransferRequest
{
    /// <summary>
    /// Gets the source location (for download) or local file path (for upload).
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the destination location (for upload) or local file path (for download).
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Gets the transfer options.
    /// </summary>
    public TransferOptions Options { get; init; } = new();

    /// <summary>
    /// Gets the credentials for authentication.
    /// </summary>
    public TransferCredentials? Credentials { get; init; }

    /// <summary>
    /// Gets the progress reporter.
    /// </summary>
    public IProgress<TransferProgress>? Progress { get; init; }

    /// <summary>
    /// Gets additional metadata for the transfer.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the correlation ID for tracking related transfers.
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the user or application identity initiating the transfer.
    /// </summary>
    public string? Identity { get; init; }

    /// <summary>
    /// Callback invoked when transfer starts.
    /// </summary>
    public Action<TransferProgress>? OnStart { get; init; }

    /// <summary>
    /// Callback invoked when a chunk is sent (upload).
    /// </summary>
    public Action<TransferProgress>? OnChunkSent { get; init; }

    /// <summary>
    /// Callback invoked when a chunk is received (download).
    /// </summary>
    public Action<TransferProgress>? OnChunkReceived { get; init; }

    /// <summary>
    /// Callback invoked when transfer completes successfully.
    /// </summary>
    public Action<TransferResult>? OnCompleted { get; init; }

    /// <summary>
    /// Callback invoked when transfer fails.
    /// </summary>
    public Action<TransferError>? OnFailed { get; init; }
}
