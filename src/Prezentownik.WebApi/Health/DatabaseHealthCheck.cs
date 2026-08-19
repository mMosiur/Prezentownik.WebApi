using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prezentownik.WebApi.Data;

namespace Prezentownik.WebApi.Health;

public sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    private const string DatabaseUnreachable = "Database is unreachable";
    private const string DatabaseMigrationsPending = "Pending database migrations";
    private const string DatabaseUpToDate = "Database is up to date";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect is false)
            {
                return HealthCheckResult.Unhealthy(DatabaseUnreachable);
            }

            var pendingMigrations = (await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken))
                .ToList();

            if (pendingMigrations.Count == 0)
            {
                return HealthCheckResult.Healthy(DatabaseUpToDate);
            }

            var pendingMigrationsText = string.Join(", ", pendingMigrations);
            logger.LogWarning("Pending database migrations: {PendingMigrations}", pendingMigrationsText);

            return HealthCheckResult.Degraded(
                description: DatabaseMigrationsPending,
                data: new Dictionary<string, object>
                {
                    ["PendingMigrations"] = pendingMigrations
                });

        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to database during health check");
            return HealthCheckResult.Unhealthy(DatabaseUnreachable, ex);
        }
    }
}
