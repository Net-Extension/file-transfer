using FileTransfer.Core.Models;

namespace FileTransfer.Providers.Axway;

/// <summary>
/// Configuration for Axway MFT connection.
/// </summary>
public sealed record AxwayConfiguration
{
    /// <summary>
    /// Gets the Axway API base URL (e.g., https://your-instance.axway.com/api/v2).
    /// </summary>
    public required string ApiBaseUrl { get; init; }

    /// <summary>
    /// Gets the authentication type (ApiKey, OAuth, Certificate).
    /// </summary>
    public AxwayAuthType AuthType { get; init; } = AxwayAuthType.ApiKey;

    /// <summary>
    /// Gets the API key for authentication.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the OAuth client ID.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the OAuth client secret.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Gets the tenant ID for multi-tenant Axway instances.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the default application name for transfers.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Gets the timeout for API calls.
    /// </summary>
    public TimeSpan ApiTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets whether to enable workflow triggers on upload.
    /// </summary>
    public bool EnableWorkflowTriggers { get; init; } = true;

    /// <summary>
    /// Gets whether to verify SSL certificates.
    /// </summary>
    public bool VerifySslCertificate { get; init; } = true;
}

/// <summary>
/// Authentication types for Axway.
/// </summary>
public enum AxwayAuthType
{
    ApiKey,
    OAuth,
    Certificate
}

/// <summary>
/// Axway file metadata.
/// </summary>
public sealed record AxwayFileMetadata
{
    /// <summary>
    /// Gets the file ID in Axway.
    /// </summary>
    public string? FileId { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Gets the MIME type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the folder path in Axway.
    /// </summary>
    public string? FolderPath { get; init; }

    /// <summary>
    /// Gets custom metadata attributes.
    /// </summary>
    public Dictionary<string, string> Attributes { get; init; } = new();

    /// <summary>
    /// Gets the workflow to trigger on upload.
    /// </summary>
    public string? WorkflowId { get; init; }

    /// <summary>
    /// Gets the expiration time for the file.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; init; }
}

/// <summary>
/// Response from Axway API.
/// </summary>
internal sealed record AxwayApiResponse
{
    public bool Success { get; init; }
    public string? FileId { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, object>? Data { get; init; }
}
