using Coldrun.Modules.Trucks.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Coldrun.HealthChecks;

/// <summary>
/// Health check that verifies the in-memory truck store is accessible.
/// For MVP, this is a trivial check. In Phase 2+, this will check database connectivity.
/// </summary>
public sealed class InMemoryStoreHealthCheck : IHealthCheck
{
    private readonly InMemoryTruckStore _store;

    public InMemoryStoreHealthCheck(InMemoryTruckStore store)
    {
        _store = store;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // For MVP: verify the store is not null and can enumerate (trivial check)
        // In Phase 2+: replace with actual database connectivity check
        try
        {
            var count = _store.GetAll().Count;
            return Task.FromResult(HealthCheckResult.Healthy($"In-memory store accessible. Truck count: {count}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("In-memory store is not accessible", ex));
        }
    }
}
