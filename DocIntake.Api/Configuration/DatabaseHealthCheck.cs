using DocIntake.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DocIntake.Api.Configuration;

/// <summary>Readiness check: can the API reach its database right now? Reports only healthy/unhealthy, never details.</summary>
public class DatabaseHealthCheck(AppDbContext db, ILogger<DatabaseHealthCheck> log) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            return await db.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database not reachable");
        }
        catch (Exception ex)
        {
            log.LogWarning("Readiness check failed: {ExceptionType}", ex.GetType().Name);
            return HealthCheckResult.Unhealthy("Database check failed");
        }
    }
}
