using System.Security.Cryptography;
using System.Text;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;

namespace FileTransfer.Core.Services;

/// <summary>
/// Default implementation of integrity verification service.
/// </summary>
public sealed class IntegrityService : IIntegrityService
{
    public async Task<string> ComputeChecksumAsync(Stream stream, ChecksumAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        if (algorithm == ChecksumAlgorithm.None)
        {
            return string.Empty;
        }

        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            using var hashAlgorithm = CreateHashAlgorithm(algorithm);
            var hashBytes = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    public async Task<bool> VerifyChecksumAsync(Stream stream, string expectedChecksum, ChecksumAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        var actualChecksum = await ComputeChecksumAsync(stream, algorithm, cancellationToken);
        return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<long> CountRecordsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            long count = 0;

            while (await reader.ReadLineAsync(cancellationToken) != null)
            {
                count++;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return count;
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    private static HashAlgorithm CreateHashAlgorithm(ChecksumAlgorithm algorithm)
    {
        return algorithm switch
        {
            ChecksumAlgorithm.Md5 => MD5.Create(),
            ChecksumAlgorithm.Sha1 => SHA1.Create(),
            ChecksumAlgorithm.Sha256 => SHA256.Create(),
            ChecksumAlgorithm.Sha512 => SHA512.Create(),
            _ => throw new NotSupportedException($"Checksum algorithm '{algorithm}' is not supported.")
        };
    }
}
