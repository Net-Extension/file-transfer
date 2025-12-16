using System.IO.Compression;
using FileTransfer.Core.Models;

namespace FileTransfer.Compression;

/// <summary>
/// Compression service using Gzip and Deflate.
/// </summary>
public sealed class GzipCompressionService : ICompressionService
{
    public async Task CompressAsync(Stream input, Stream output, CompressionAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        Stream compressionStream = algorithm switch
        {
            CompressionAlgorithm.Gzip => new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true),
            CompressionAlgorithm.Deflate => new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true),
            CompressionAlgorithm.Brotli => new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true),
            _ => throw new NotSupportedException($"Compression algorithm '{algorithm}' is not supported.")
        };

        await using (compressionStream)
        {
            await input.CopyToAsync(compressionStream, cancellationToken);
        }
    }

    public async Task DecompressAsync(Stream input, Stream output, CompressionAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        Stream decompressionStream = algorithm switch
        {
            CompressionAlgorithm.Gzip => new GZipStream(input, CompressionMode.Decompress, leaveOpen: true),
            CompressionAlgorithm.Deflate => new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true),
            CompressionAlgorithm.Brotli => new BrotliStream(input, CompressionMode.Decompress, leaveOpen: true),
            _ => throw new NotSupportedException($"Compression algorithm '{algorithm}' is not supported.")
        };

        await using (decompressionStream)
        {
            await decompressionStream.CopyToAsync(output, cancellationToken);
        }
    }
}
