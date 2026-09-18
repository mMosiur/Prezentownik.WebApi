using Prezentownik.WebApi.Data;

namespace Prezentownik.WebApi.HostedServices;

internal sealed class DbStartupCheckHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DbStartupCheckHostedService> logger)
    : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ILogger<DbStartupCheckHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogError("Database connection startup check failed");
                return;
            }

            _logger.LogInformation("Database connection and migrations startup check completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while connecting to the database on startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
