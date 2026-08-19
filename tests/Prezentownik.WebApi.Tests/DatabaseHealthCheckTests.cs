using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Health;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql("Host=127.0.0.1;Port=59999;Database=nonexistent;Username=fake;Password=fake;Timeout=1;Command Timeout=1"));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<DatabaseHealthCheck>.Instance;

        var healthCheck = new DatabaseHealthCheck(scopeFactory, logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database is unreachable", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInMemoryProviderThrowsRelationalException_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<DatabaseHealthCheck>.Instance;

        var healthCheck = new DatabaseHealthCheck(scopeFactory, logger);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("Database is unreachable", result.Description);
    }
}
