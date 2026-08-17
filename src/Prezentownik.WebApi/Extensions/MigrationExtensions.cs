using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;

namespace Prezentownik.WebApi.Extensions;

public static class MigrationExtensions
{
    extension(IApplicationBuilder app)
    {
        public async Task ApplyMigrationsAsync()
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            await using AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        public async Task CheckMigrationsAsync()
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            await using AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count > 0)
            {
                throw new InvalidOperationException($"Database is not up to date. Pending migrations: {string.Join(", ", pendingMigrations)}");
            }
        }
    }
}
