using System.Security.Cryptography;
using FileTransfer.Core.Models;

namespace FileTransfer.Security;

/// <summary>
/// Encryption service using AES-GCM.
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16;   // 128 bits

    public async Task EncryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default)
    {
        if (algorithm != EncryptionAlgorithm.AesGcm)
        {
            throw new NotSupportedException($"Algorithm {algorithm} is not supported by this service.");
        }

        ValidateKey(key);

        // Generate random nonce
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Write nonce to output
        await output.WriteAsync(nonce, cancellationToken);

        // Read all input data (in production, consider chunked encryption for large files)
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, cancellationToken);
        var plaintext = ms.ToArray();

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        // Write tag
        await output.WriteAsync(tag, cancellationToken);

        // Write ciphertext
        await output.WriteAsync(ciphertext, cancellationToken);
    }

    public async Task DecryptAsync(Stream input, Stream output, EncryptionAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default)
    {
        if (algorithm != EncryptionAlgorithm.AesGcm)
        {
            throw new NotSupportedException($"Algorithm {algorithm} is not supported by this service.");
        }

        ValidateKey(key);

        // Read nonce
        var nonce = new byte[NonceSize];
        await input.ReadExactlyAsync(nonce, cancellationToken);

        // Read tag
        var tag = new byte[TagSize];
        await input.ReadExactlyAsync(tag, cancellationToken);

        // Read ciphertext
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, cancellationToken);
        var ciphertext = ms.ToArray();

        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        await output.WriteAsync(plaintext, cancellationToken);
    }

    public byte[] GenerateKey(EncryptionAlgorithm algorithm)
    {
        if (algorithm != EncryptionAlgorithm.AesGcm)
        {
            throw new NotSupportedException($"Algorithm {algorithm} is not supported by this service.");
        }

        var key = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static void ValidateKey(byte[] key)
    {
        if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
        {
            throw new ArgumentException("Key must be 128, 192, or 256 bits (16, 24, or 32 bytes).", nameof(key));
        }
    }
}
