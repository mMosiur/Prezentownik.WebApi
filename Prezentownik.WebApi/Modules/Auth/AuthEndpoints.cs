using Prezentownik.WebApi.Models;

namespace Prezentownik.WebApi.Modules.Auth;

public static class AuthEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGroup("/auth").MapIdentityApi<AppUser>();
    }
}
