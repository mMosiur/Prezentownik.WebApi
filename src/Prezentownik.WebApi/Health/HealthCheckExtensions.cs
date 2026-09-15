using System.Diagnostics.CodeAnalysis;

namespace Prezentownik.WebApi.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["integration", "db"]);

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationHealthChecks(
        this IEndpointRouteBuilder app,
        [StringSyntax("Route")] string basePattern)
    {
        app.MapHealthChecks($"{basePattern}", new()
        {
            AllowCachingResponses = false,
            Predicate = healthCheck => healthCheck.Tags.Contains("integration") is false
        });

        app.MapHealthChecks($"{basePattern}/db", new()
        {
            AllowCachingResponses = false,
            Predicate = healthCheck => healthCheck.Tags.Contains("db") is true
        });

        app.MapHealthChecks($"{basePattern}/integrations", new()
        {
            AllowCachingResponses = false,
            Predicate = healthCheck => healthCheck.Tags.Contains("integration") is true
        });

        return app;
    }
}
