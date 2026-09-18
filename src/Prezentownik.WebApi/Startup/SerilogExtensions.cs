using Serilog;
using Serilog.Events;

namespace Prezentownik.WebApi.Startup;

internal static class SerilogExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSerilog(IWebHostEnvironment environment)
            => environment.IsDevelopment()
                ? services.AddDevelopmentSerilog()
                : services.AddProductionSerilog();

        private IServiceCollection AddDevelopmentSerilog()
            => services.AddSerilog((sp, lc) => lc
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(path: "logs/log-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = Diagnostics.ServiceName
                    };
                }));

        private IServiceCollection AddProductionSerilog()
            => services.AddSerilog((sp, lc) => lc
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.OpenTelemetry(options =>
                {
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = Diagnostics.ServiceName
                    };
                }));
    }
}
