using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileTransfer.Auditing;

/// <summary>
/// Audit sink that writes to a file.
/// </summary>
public sealed class FileAuditSink : IAuditSink, IDisposable
{
    private readonly FileAuditSinkOptions _options;
    private readonly ILogger<FileAuditSink> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileAuditSink(IOptions<FileAuditSinkOptions> options, ILogger<FileAuditSink> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_options.FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(auditEvent, _jsonOptions);
            await File.AppendAllLinesAsync(_options.FilePath, new[] { json }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit event to file: {FilePath}", _options.FilePath);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var lines = auditEvents.Select(e => JsonSerializer.Serialize(e, _jsonOptions));
            await File.AppendAllLinesAsync(_options.FilePath, lines, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit events batch to file: {FilePath}", _options.FilePath);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // File I/O is already flushed after each write
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}

/// <summary>
/// Configuration options for file audit sink.
/// </summary>
public sealed class FileAuditSinkOptions
{
    public string FilePath { get; set; } = "logs/audit.log";
}
