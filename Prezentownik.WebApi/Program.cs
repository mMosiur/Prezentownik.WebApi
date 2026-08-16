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
using Prezentownik.WebApi.Extensions;
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

    builder.Services.AddHealthChecks();

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
            // Weak password allowed, for testing purposes only for now
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>();

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

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
        await app.ApplyMigrationsAsync();
    }
    else
    {
        await app.CheckMigrationsAsync();
    }

    app.MapHealthChecks("healthz");

    app.UseExceptionHandler();

    app.UseCors();

    app.UseSerilogRequestLogging();

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
