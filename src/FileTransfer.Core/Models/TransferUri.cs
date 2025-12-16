namespace FileTransfer.Core.Models;

/// <summary>
/// Represents a typed URI for file transfers with parsed components.
/// </summary>
public sealed record TransferUri
{
    /// <summary>
    /// Gets the scheme (protocol) - http, https, ftp, sftp, scp, ws, wss, custom.
    /// </summary>
    public required string Scheme { get; init; }

    /// <summary>
    /// Gets the host name or IP address.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Gets the port number (null for default port).
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the query string parameters.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Gets the username if specified in the URI.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the full URI string.
    /// </summary>
    public string FullUri => BuildFullUri();

    /// <summary>
    /// Creates a TransferUri from a string URI.
    /// </summary>
    public static TransferUri Parse(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            throw new ArgumentException("URI string cannot be null or empty.", nameof(uriString));
        }

        var uri = new Uri(uriString);

        return new TransferUri
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host,
            Port = uri.Port != -1 && !uri.IsDefaultPort ? uri.Port : null,
            Path = uri.AbsolutePath,
            Query = string.IsNullOrEmpty(uri.Query) ? null : uri.Query.TrimStart('?'),
            Username = string.IsNullOrEmpty(uri.UserInfo) ? null : uri.UserInfo.Split(':')[0]
        };
    }

    /// <summary>
    /// Tries to parse a string URI.
    /// </summary>
    public static bool TryParse(string uriString, out TransferUri? transferUri)
    {
        try
        {
            transferUri = Parse(uriString);
            return true;
        }
        catch
        {
            transferUri = null;
            return false;
        }
    }

    private string BuildFullUri()
    {
        var builder = new UriBuilder
        {
            Scheme = Scheme,
            Host = Host,
            Port = Port ?? -1,
            Path = Path
        };

        if (!string.IsNullOrEmpty(Query))
        {
            builder.Query = Query;
        }

        if (!string.IsNullOrEmpty(Username))
        {
            builder.UserName = Username;
        }

        return builder.Uri.ToString();
    }
}
