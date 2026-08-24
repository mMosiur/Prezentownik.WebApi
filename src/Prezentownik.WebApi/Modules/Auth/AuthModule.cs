using Microsoft.AspNetCore.Identity;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Auth.Email;

namespace Prezentownik.WebApi.Modules.Auth;

public abstract class AuthModule : IModule
{
    public static void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IIdentityEmailTemplateService, IdentityEmailTemplateService>();
        builder.Services.AddTransient<IEmailSender<AppUser>, IdentityEmailSender>();
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => AuthEndpoints.MapEndpoints(app);
}
