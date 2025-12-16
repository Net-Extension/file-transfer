namespace FileTransfer.Auditing;

/// <summary>
/// Composite audit sink that writes to multiple sinks.
/// </summary>
public sealed class CompositeAuditSink : IAuditSink
{
    private readonly IEnumerable<IAuditSink> _sinks;

    public CompositeAuditSink(IEnumerable<IAuditSink> sinks)
    {
        _sinks = sinks;
    }

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var tasks = _sinks.Select(sink => sink.WriteAsync(auditEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        var tasks = _sinks.Select(sink => sink.WriteBatchAsync(auditEvents, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _sinks.Select(sink => sink.FlushAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }
}
