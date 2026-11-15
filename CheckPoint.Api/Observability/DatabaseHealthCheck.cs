using CheckPoint.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CheckPoint.Api.Observability;

public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            return await db.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
