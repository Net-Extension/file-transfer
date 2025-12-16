namespace FileTransfer.Core.Models;

/// <summary>
/// Represents credentials for authentication.
/// </summary>
public sealed record TransferCredentials
{
    /// <summary>
    /// Gets the credential type.
    /// </summary>
    public required CredentialType Type { get; init; }

    /// <summary>
    /// Gets the username (for UsernamePassword).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the password (for UsernamePassword).
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets the bearer token (for BearerToken).
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Gets the private key content (for PrivateKey).
    /// </summary>
    public string? PrivateKey { get; init; }

    /// <summary>
    /// Gets the private key passphrase.
    /// </summary>
    public string? Passphrase { get; init; }

    /// <summary>
    /// Gets the certificate thumbprint or path (for Certificate).
    /// </summary>
    public string? Certificate { get; init; }

    /// <summary>
    /// Gets the API key (for ApiKey).
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets custom credential data.
    /// </summary>
    public Dictionary<string, string>? CustomData { get; init; }

    /// <summary>
    /// Creates username/password credentials.
    /// </summary>
    public static TransferCredentials CreateUsernamePassword(string username, string password)
    {
        return new TransferCredentials
        {
            Type = CredentialType.UsernamePassword,
            Username = username,
            Password = password
        };
    }

    /// <summary>
    /// Creates bearer token credentials.
    /// </summary>
    public static TransferCredentials CreateBearerToken(string token)
    {
        return new TransferCredentials
        {
            Type = CredentialType.BearerToken,
            Token = token
        };
    }

    /// <summary>
    /// Creates private key credentials.
    /// </summary>
    public static TransferCredentials CreatePrivateKey(string privateKey, string? passphrase = null)
    {
        return new TransferCredentials
        {
            Type = CredentialType.PrivateKey,
            PrivateKey = privateKey,
            Passphrase = passphrase
        };
    }

    /// <summary>
    /// Creates API key credentials.
    /// </summary>
    public static TransferCredentials CreateApiKey(string apiKey)
    {
        return new TransferCredentials
        {
            Type = CredentialType.ApiKey,
            ApiKey = apiKey
        };
    }
}
