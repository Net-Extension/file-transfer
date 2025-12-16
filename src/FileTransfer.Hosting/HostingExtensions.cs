using FileTransfer.Hosting.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransfer.Hosting;

/// <summary>
/// Extension methods for hosting scenarios.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds FileTransfer health checks.
    /// </summary>
    public static IHealthChecksBuilder AddFileTransferHealthChecks(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<FileTransferHealthCheck>("file_transfer", tags: new[] { "ready", "live" });
    }
}
