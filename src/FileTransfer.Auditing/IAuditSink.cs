namespace FileTransfer.Auditing;

/// <summary>
/// Sink for persisting audit events.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Writes an audit event to the sink.
    /// </summary>
    /// <param name="auditEvent">The audit event to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple audit events to the sink.
    /// </summary>
    /// <param name="auditEvents">The audit events to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered audit events to storage.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
