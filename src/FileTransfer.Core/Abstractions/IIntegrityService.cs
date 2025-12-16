using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Service for computing and verifying file integrity checksums.
/// </summary>
public interface IIntegrityService
{
    /// <summary>
    /// Computes a checksum for a stream.
    /// </summary>
    /// <param name="stream">The stream to compute checksum for.</param>
    /// <param name="algorithm">The hash algorithm to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed checksum.</returns>
    Task<string> ComputeChecksumAsync(Stream stream, ChecksumAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that a stream matches the expected checksum.
    /// </summary>
    /// <param name="stream">The stream to verify.</param>
    /// <param name="expectedChecksum">The expected checksum value.</param>
    /// <param name="algorithm">The hash algorithm to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the checksum matches, false otherwise.</returns>
    Task<bool> VerifyChecksumAsync(Stream stream, string expectedChecksum, ChecksumAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts records in a line-based file (CSV, text, etc.) in a streaming manner.
    /// </summary>
    /// <param name="stream">The stream to count records in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records (lines) in the file.</returns>
    Task<long> CountRecordsAsync(Stream stream, CancellationToken cancellationToken = default);
}
