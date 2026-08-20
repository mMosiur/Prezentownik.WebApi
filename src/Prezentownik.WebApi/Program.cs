using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules;
using Prezentownik.WebApi.Modules.Auth;
using Prezentownik.WebApi.Modules.Public;
using Prezentownik.WebApi.Modules.UserLists;
using Prezentownik.WebApi.Health;
using Prezentownik.WebApi;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.OpenTelemetry(options =>
        {
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = Diagnostics.ServiceName
            };
        }));

    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails();

    builder.Services.AddValidation();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(_ => true);
            }
            else
            {
                var allowedOrigins = builder.Configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];
                policy.WithOrigins(allowedOrigins);
            }

            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(Diagnostics.ServiceName))
        .WithTracing(tracing => tracing
            .AddSource(Diagnostics.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddNpgsql()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter());

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.MapApplicationEnums(schema: "app")));

    builder.Services.AddIdentityApiEndpoints<AppUser>(options =>
        {
            // The audience is non-technical family members, so we deliberately
            // skip character-variety rules (uppercase/digit/symbol) to keep
            // sign-up friction-free. A longer minimum length is a reasonable
            // trade-off that still meaningfully improves security.
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>();

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Anonymous visitors claim/unclaim gifts without an account, so throttle
        // by IP to stop accidental double-submits (flaky mobile connections)
        // and deliberate spam from overwhelming a list's claims.
        options.AddPolicy("public-claims", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    builder.Services.RegisterModuleServices<AuthModule>();
    builder.Services.RegisterModuleServices<UserListsModule>();
    builder.Services.RegisterModuleServices<PublicModule>();

    var app = builder.Build();

    app.UseForwardedHeaders(new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto,
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapHealthChecks("health");

    app.UseExceptionHandler();

    app.UseCors();

    app.UseSerilogRequestLogging();

    app.UseRateLimiter();

    app.UseAuthorization();

    app.MapModuleEndpoints<AuthModule>();
    app.MapModuleEndpoints<UserListsModule>();
    app.MapModuleEndpoints<PublicModule>();


    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
