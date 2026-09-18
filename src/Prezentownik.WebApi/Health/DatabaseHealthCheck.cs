using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prezentownik.WebApi.Data;

namespace Prezentownik.WebApi.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    : IHealthCheck
{
    private const string DatabaseUnreachable = "Database is unreachable";
    private const string DatabaseAvailable = "Database connection successful";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseHealthCheck>>();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(DatabaseUnreachable);
            }

            return HealthCheckResult.Healthy(DatabaseAvailable);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to database during health check");
            return HealthCheckResult.Unhealthy(DatabaseUnreachable, ex);
        }
    }
}
