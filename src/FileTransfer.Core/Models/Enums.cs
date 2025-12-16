namespace FileTransfer.Core.Models;

/// <summary>
/// Checksum algorithms for integrity verification.
/// </summary>
public enum ChecksumAlgorithm
{
    None,
    Md5,
    Sha1,
    Sha256,
    Sha512
}

/// <summary>
/// Compression algorithms.
/// </summary>
public enum CompressionAlgorithm
{
    None,
    Gzip,
    Zip,
    Deflate,
    Brotli
}

/// <summary>
/// Encryption algorithms.
/// </summary>
public enum EncryptionAlgorithm
{
    None,
    AesGcm,
    AesCbc,
    ChaCha20Poly1305
}

/// <summary>
/// TLS versions for secure transfers.
/// </summary>
public enum TlsVersion
{
    Tls10,
    Tls11,
    Tls12,
    Tls13
}

/// <summary>
/// Transfer state during operation.
/// </summary>
public enum TransferState
{
    Pending,
    Validating,
    Starting,
    InProgress,
    Paused,
    Resuming,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Error codes for transfer failures.
/// </summary>
public enum TransferErrorCode
{
    Unknown,
    NetworkError,
    AuthenticationFailed,
    AuthorizationFailed,
    ValidationFailed,
    IntegrityCheckFailed,
    TimeoutExpired,
    OperationCancelled,
    FileNotFound,
    FileAccessDenied,
    InsufficientSpace,
    UnsupportedProtocol,
    InvalidCredentials,
    ConnectionRefused,
    HostUnreachable,
    PathTraversalDetected,
    FileSizeTooLarge,
    FileExtensionNotAllowed,
    HostNotAllowed,
    TlsVersionNotSupported,
    EncryptionKeyMissing,
    CompressionFailed,
    DecompressionFailed
}

/// <summary>
/// Credential types for authentication.
/// </summary>
public enum CredentialType
{
    None,
    UsernamePassword,
    BearerToken,
    PrivateKey,
    Certificate,
    ApiKey,
    Custom
}
