using Microsoft.AspNetCore.Identity;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Auth.Email;

namespace Prezentownik.WebApi.Modules.Auth;

public abstract class AuthModule : IModule
{
    public static void RegisterModule(IHostApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            builder.Services.AddTransient<IEmailSender<AppUser>, LoggingOnlyConfirmationEmailSender>();
        else
            builder.Services.AddTransient<IEmailSender<AppUser>, ConfirmationEmailSender>();
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => AuthEndpoints.MapEndpoints(app);
}
