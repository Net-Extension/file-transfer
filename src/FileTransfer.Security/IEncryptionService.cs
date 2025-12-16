using FileTransfer.Core.Models;

namespace FileTransfer.Security;

/// <summary>
/// Service for encrypting and decrypting streams.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a stream.
    /// </summary>
    /// <param name="input">The input stream to encrypt.</param>
    /// <param name="output">The output stream for encrypted data.</param>
    /// <param name="algorithm">The encryption algorithm to use.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EncryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts a stream.
    /// </summary>
    /// <param name="input">The encrypted input stream.</param>
    /// <param name="output">The output stream for decrypted data.</param>
    /// <param name="algorithm">The encryption algorithm used.</param>
    /// <param name="key">The decryption key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DecryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a random encryption key of appropriate size for the algorithm.
    /// </summary>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>A randomly generated key.</returns>
    byte[] GenerateKey(EncryptionAlgorithm algorithm);
}
