using System.Text.RegularExpressions;

namespace FileTransfer.Security;

/// <summary>
/// Utility for redacting secrets from strings for safe logging.
/// </summary>
public static partial class SecretRedactor
{
    private const string RedactedText = "***REDACTED***";

    /// <summary>
    /// Redacts common secret patterns from a string.
    /// </summary>
    public static string RedactSecrets(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        // Redact passwords in connection strings
        result = PasswordPattern().Replace(result, $"password={RedactedText}");

        // Redact bearer tokens
        result = BearerTokenPattern().Replace(result, $"Bearer {RedactedText}");

        // Redact API keys
        result = ApiKeyPattern().Replace(result, $"apikey={RedactedText}");

        // Redact Authorization headers
        result = AuthHeaderPattern().Replace(result, $"Authorization: {RedactedText}");

        return result;
    }

    /// <summary>
    /// Redacts a password, showing only the first and last character if longer than 6 chars.
    /// </summary>
    public static string RedactPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return RedactedText;
        }

        if (password.Length <= 6)
        {
            return RedactedText;
        }

        return $"{password[0]}***{password[^1]}";
    }

    /// <summary>
    /// Redacts a token, showing only the last 4 characters.
    /// </summary>
    public static string RedactToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length <= 4)
        {
            return RedactedText;
        }

        return $"***{token[^4..]}";
    }

    [GeneratedRegex(@"password=[^;]*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"apikey=[^&\s]*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"Authorization:\s*[^\r\n]*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AuthHeaderPattern();
}
