using FileTransfer.Core.Models;

namespace FileTransfer.Core.Abstractions;

/// <summary>
/// Defines retry and resilience policies for transfer operations.
/// </summary>
public interface ITransferPolicy
{
    /// <summary>
    /// Gets the retry configuration.
    /// </summary>
    RetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Determines if an exception is transient and should be retried.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>True if the exception is transient and should be retried.</returns>
    bool IsTransient(Exception exception);

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <returns>The delay before retrying.</returns>
    TimeSpan GetRetryDelay(int attemptNumber);
}
