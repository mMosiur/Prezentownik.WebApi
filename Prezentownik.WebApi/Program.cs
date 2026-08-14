using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules;
using Prezentownik.WebApi.Modules.Auth;
using Prezentownik.WebApi.Modules.Public;
using Prezentownik.WebApi.Modules.UserLists;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

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
}

app.MapHealthChecks("healthz");

app.UseAuthorization();

app.MapModuleEndpoints<AuthModule>();
app.MapModuleEndpoints<UserListsModule>();
app.MapModuleEndpoints<PublicModule>();


app.Run();
