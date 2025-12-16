using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;

namespace FileTransfer.Core.Services;

/// <summary>
/// Default implementation of transfer policy with exponential backoff retry logic.
/// </summary>
public sealed class DefaultTransferPolicy : ITransferPolicy
{
    private static readonly Random _random = new();

    public RetryPolicy RetryPolicy { get; }

    public DefaultTransferPolicy(RetryPolicy? retryPolicy = null)
    {
        RetryPolicy = retryPolicy ?? RetryPolicy.Default;
    }

    public bool IsTransient(Exception exception)
    {
        return exception is IOException
            or TimeoutException
            or HttpRequestException
            or OperationCanceledException
            or SocketException;
    }

    public TimeSpan GetRetryDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            return TimeSpan.Zero;
        }

        TimeSpan delay;

        if (RetryPolicy.UseExponentialBackoff)
        {
            var exponentialDelay = RetryPolicy.InitialDelay.TotalMilliseconds
                * Math.Pow(RetryPolicy.BackoffMultiplier, attemptNumber - 1);
            delay = TimeSpan.FromMilliseconds(Math.Min(exponentialDelay, RetryPolicy.MaxDelay.TotalMilliseconds));
        }
        else
        {
            delay = RetryPolicy.InitialDelay;
        }

        if (RetryPolicy.UseJitter)
        {
            var jitter = _random.NextDouble() * delay.TotalMilliseconds * 0.2; // ±20% jitter
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter - (delay.TotalMilliseconds * 0.1));
        }

        return delay;
    }
}
