using FileTransfer.Core.Models;

namespace FileTransfer.Auditing;

/// <summary>
/// Represents an audit event for a file transfer operation.
/// </summary>
public sealed record AuditEvent
{
    /// <summary>
    /// Gets the unique event ID.
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the correlation ID linking related operations.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the type of audit event.
    /// </summary>
    public required AuditEventType EventType { get; init; }

    /// <summary>
    /// Gets the user or application identity.
    /// </summary>
    public string? Identity { get; init; }

    /// <summary>
    /// Gets the protocol/scheme used.
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets the source endpoint/path.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the destination endpoint/path.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Gets the operation (Upload or Download).
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the number of bytes transferred.
    /// </summary>
    public long? BytesTransferred { get; init; }

    /// <summary>
    /// Gets the total file size.
    /// </summary>
    public long? TotalBytes { get; init; }

    /// <summary>
    /// Gets the duration of the operation.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the result (Success or Failure).
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Gets the checksum value.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the checksum algorithm used.
    /// </summary>
    public ChecksumAlgorithm? ChecksumAlgorithm { get; init; }

    /// <summary>
    /// Gets the record count for line-based files.
    /// </summary>
    public long? RecordCount { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed.
    /// </summary>
    public TransferErrorCode? ErrorCode { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the host name where the transfer occurred.
    /// </summary>
    public string? HostName { get; init; }

    /// <summary>
    /// Creates an audit event for a transfer start.
    /// </summary>
    public static AuditEvent CreateStarted(TransferRequest request, string protocol, long? totalBytes = null)
    {
        return new AuditEvent
        {
            CorrelationId = request.CorrelationId,
            EventType = AuditEventType.TransferStarted,
            Identity = request.Identity,
            Protocol = protocol,
            Source = request.Source,
            Destination = request.Destination,
            Operation = DetermineOperation(request),
            TotalBytes = totalBytes,
            HostName = Environment.MachineName,
            Metadata = new Dictionary<string, string>(request.Metadata)
        };
    }

    /// <summary>
    /// Creates an audit event for a transfer completion.
    /// </summary>
    public static AuditEvent CreateCompleted(TransferResult result)
    {
        return new AuditEvent
        {
            CorrelationId = result.CorrelationId,
            EventType = AuditEventType.TransferCompleted,
            Protocol = result.Protocol,
            Source = result.Source,
            Destination = result.Destination,
            Operation = "Transfer",
            BytesTransferred = result.BytesTransferred,
            TotalBytes = result.TotalBytes,
            Duration = result.Duration,
            Result = result.Success ? "Success" : "Failure",
            Checksum = result.Checksum,
            ChecksumAlgorithm = result.ChecksumAlgorithm,
            RecordCount = result.RecordCount,
            ErrorCode = result.Error?.Code,
            ErrorMessage = result.Error?.Message,
            HostName = Environment.MachineName,
            Metadata = new Dictionary<string, string>(result.Metadata)
        };
    }

    /// <summary>
    /// Creates an audit event for a transfer failure.
    /// </summary>
    public static AuditEvent CreateFailed(TransferRequest request, string protocol, TransferError error, TimeSpan? duration = null)
    {
        return new AuditEvent
        {
            CorrelationId = request.CorrelationId,
            EventType = AuditEventType.TransferFailed,
            Identity = request.Identity,
            Protocol = protocol,
            Source = request.Source,
            Destination = request.Destination,
            Operation = DetermineOperation(request),
            Result = "Failure",
            ErrorCode = error.Code,
            ErrorMessage = error.Message,
            Duration = duration,
            HostName = Environment.MachineName,
            Metadata = new Dictionary<string, string>(request.Metadata)
        };
    }

    private static string DetermineOperation(TransferRequest request)
    {
        // This is a simplification - in a real implementation, you'd determine this from context
        return "Transfer";
    }
}

/// <summary>
/// Types of audit events.
/// </summary>
public enum AuditEventType
{
    TransferStarted,
    TransferCompleted,
    TransferFailed,
    TransferCancelled,
    ValidationFailed,
    AuthenticationAttempt,
    AuthenticationFailed
}
