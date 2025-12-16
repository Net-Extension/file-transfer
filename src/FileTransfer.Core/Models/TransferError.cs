namespace FileTransfer.Core.Models;

/// <summary>
/// Represents an error that occurred during file transfer.
/// </summary>
public sealed record TransferError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public required TransferErrorCode Code { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the exception that caused the error.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets whether the error is transient and can be retried.
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public Dictionary<string, string> Details { get; init; } = new();

    /// <summary>
    /// Creates an error from an exception.
    /// </summary>
    public static TransferError FromException(Exception exception, TransferErrorCode code = TransferErrorCode.Unknown, bool isTransient = false)
    {
        return new TransferError
        {
            Code = code,
            Message = exception.Message,
            Exception = exception,
            IsTransient = isTransient
        };
    }
}
