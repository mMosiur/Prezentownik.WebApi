namespace Prezentownik.WebApi.Modules.Public;

public abstract class PublicModule : IModule
{
    public static void RegisterModule(IHostApplicationBuilder builder)
    {
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => PublicEndpoints.MapEndpoints(app);
}
