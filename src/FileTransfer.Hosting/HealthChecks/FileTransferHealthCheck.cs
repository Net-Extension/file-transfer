using FileTransfer.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileTransfer.Hosting.HealthChecks;

/// <summary>
/// Health check for FileTransfer service availability.
/// </summary>
public sealed class FileTransferHealthCheck : IHealthCheck
{
    private readonly ITransferProviderFactory _factory;

    public FileTransferHealthCheck(ITransferProviderFactory factory)
    {
        _factory = factory;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var providers = _factory.GetAllProviders().ToList();

            if (providers.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("No transfer providers are registered."));
            }

            var data = new Dictionary<string, object>
            {
                ["ProviderCount"] = providers.Count,
                ["Schemes"] = string.Join(", ", providers.Select(p => p.Scheme))
            };

            return Task.FromResult(HealthCheckResult.Healthy($"FileTransfer service is healthy with {providers.Count} provider(s).", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("FileTransfer service is unhealthy.", ex));
        }
    }
}
