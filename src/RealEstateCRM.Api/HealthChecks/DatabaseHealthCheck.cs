using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Api.HealthChecks;

/// <summary>
/// Readiness check: can this instance actually reach its database?
///
/// Registered under the "ready" tag only, deliberately kept out of the liveness probe. If the
/// database is unreachable the correct response is to stop routing traffic to the instance, not to
/// kill and restart it — a restart does nothing for a database that is down except add reconnection
/// load to something already struggling, which is how a brief database blip becomes a restart storm
/// across every replica at once.
/// </summary>
public sealed class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Could not connect to the database.");
        }
        catch (Exception exception)
        {
            // Returns Unhealthy rather than letting the exception escape: the probe must always
            // answer with a status. A bare 500 does not distinguish "the app is down" from "the
            // database is down", which is the one distinction this endpoint exists to make.
            return HealthCheckResult.Unhealthy("Database health check failed.", exception);
        }
    }
}
