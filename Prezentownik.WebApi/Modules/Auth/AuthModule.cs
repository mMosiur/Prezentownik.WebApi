namespace Prezentownik.WebApi.Modules.Auth;

public abstract class AuthModule : IModule
{
    public static void RegisterServices(IServiceCollection services)
    {
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => AuthEndpoints.MapEndpoints(app);
}
