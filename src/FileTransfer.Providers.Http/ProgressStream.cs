namespace FileTransfer.Providers.Http;

/// <summary>
/// Stream wrapper that reports progress as data is read.
/// </summary>
internal sealed class ProgressStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _totalBytes;
    private readonly Action<(long BytesTransferred, double RateBytesPerSecond)> _progressCallback;
    private long _bytesTransferred;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    public ProgressStream(Stream innerStream, long totalBytes, Action<(long BytesTransferred, double RateBytesPerSecond)> progressCallback)
    {
        _innerStream = innerStream;
        _totalBytes = totalBytes;
        _progressCallback = progressCallback;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesTransferred += bytesRead;

        var rate = _stopwatch.Elapsed.TotalSeconds > 0
            ? _bytesTransferred / _stopwatch.Elapsed.TotalSeconds
            : 0;

        _progressCallback((_bytesTransferred, rate));
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _bytesTransferred += bytesRead;

        var rate = _stopwatch.Elapsed.TotalSeconds > 0
            ? _bytesTransferred / _stopwatch.Elapsed.TotalSeconds
            : 0;

        _progressCallback((_bytesTransferred, rate));
        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stopwatch.Stop();
        }
        base.Dispose(disposing);
    }
}
