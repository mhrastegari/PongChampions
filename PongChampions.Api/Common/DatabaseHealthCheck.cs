using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PongChampions.Data;

namespace PongChampions.Api.Common;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        try
        {
            return await db.Database.CanConnectAsync(token) &&
                    !(await db.Database.GetPendingMigrationsAsync(token)).Any()
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database unavailable or migrations pending.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database unavailable.");
        }
    }
}
