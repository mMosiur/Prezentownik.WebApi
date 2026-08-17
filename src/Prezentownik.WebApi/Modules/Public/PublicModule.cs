namespace Prezentownik.WebApi.Modules.Public;

public abstract class PublicModule : IModule
{
    public static void RegisterServices(IServiceCollection services)
    {
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => PublicEndpoints.MapEndpoints(app);
}
