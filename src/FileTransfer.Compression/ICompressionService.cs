using FileTransfer.Core.Models;

namespace FileTransfer.Compression;

/// <summary>
/// Service for compressing and decompressing streams.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Compresses a stream.
    /// </summary>
    /// <param name="input">The input stream to compress.</param>
    /// <param name="output">The output stream for compressed data.</param>
    /// <param name="algorithm">The compression algorithm to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompressAsync(Stream input, Stream output, CompressionAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses a stream.
    /// </summary>
    /// <param name="input">The compressed input stream.</param>
    /// <param name="output">The output stream for decompressed data.</param>
    /// <param name="algorithm">The compression algorithm used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DecompressAsync(Stream input, Stream output, CompressionAlgorithm algorithm, CancellationToken cancellationToken = default);
}
