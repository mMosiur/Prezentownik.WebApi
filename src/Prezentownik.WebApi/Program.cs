using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
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
using Prezentownik.WebApi.HostedServices;
using Prezentownik.WebApi.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Building web host");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddProblemDetails();

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

    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("OpenAPI", policy => policy.Expire(TimeSpan.FromDays(1)));
    });

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

    builder.Services.AddApplicationHealthChecks();

    var otel = builder.Services.AddOpenTelemetry();

    var applicationInsightsConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
    {
        otel.UseAzureMonitor();
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear(); // Critical: Accept headers from non-localhost proxies
        options.KnownProxies.Clear();
    });

    otel.ConfigureResource(resource => resource.AddService(Diagnostics.ServiceName))
        .WithTracing(tracing => tracing
            .AddSource(Diagnostics.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddNpgsql())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation());

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.MapApplicationEnums(schema: "app")));

    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

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

            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

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

    builder.Services.AddHostedService<DbStartupCheckHostedService>();

    builder
        .AddApplicationServices()
        .RegisterModule<AuthModule>()
        .RegisterModule<UserListsModule>()
        .RegisterModule<PublicModule>();

    Log.Information("Configuring web host");
    var app = builder.Build();

    app.UseForwardedHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi("/openapi/{documentName}.yaml")
            .CacheOutput(policyName: "OpenAPI");
    }

    app.MapApplicationHealthChecks("health");

    app.UseExceptionHandler();

    app.UseCors();

    app.UseSerilogRequestLogging();

    app.UseRateLimiter();

    var supportedCultures = new[] { "pl", "pl-PL", "en", "en-US" };
    app.UseRequestLocalization(options => options
        .SetDefaultCulture("pl")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures));

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseOutputCache();

    // Simulate server latency only for local development
    int simulateLatencyInMilliseconds = app.Configuration.GetValue("SimulateLatencyInMilliseconds", defaultValue: 0);
    if (simulateLatencyInMilliseconds > 0)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Value?.Contains("health") is not true) // Skip health checks
            {
                // Adds an artificial delay
                await Task.Delay(simulateLatencyInMilliseconds);
            }
            await next();
        });
    }

    app
        .MapModuleEndpoints<AuthModule>()
        .MapModuleEndpoints<UserListsModule>()
        .MapModuleEndpoints<PublicModule>();

    Log.Information("Starting web host");
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
