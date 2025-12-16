namespace FileTransfer.Core.Models;

/// <summary>
/// Defines retry policy configuration.
/// </summary>
public sealed record RetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the initial delay before the first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Gets whether to use exponential backoff (true) or fixed delay (false).
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Gets whether to add jitter to retry delays to prevent thundering herd.
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Creates a default retry policy with exponential backoff.
    /// </summary>
    public static RetryPolicy Default => new();

    /// <summary>
    /// Creates a retry policy with no retries.
    /// </summary>
    public static RetryPolicy NoRetry => new() { MaxAttempts = 0 };
}
