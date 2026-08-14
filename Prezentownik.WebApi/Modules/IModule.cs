namespace Prezentownik.WebApi.Modules;

public interface IModule
{
    public static abstract void RegisterServices(IServiceCollection services);
    public static abstract void MapEndpoints(IEndpointRouteBuilder app);
}

public static class ModuleExtensions
{
    public static void RegisterModuleServices<TModule>(this IServiceCollection services)
        where TModule : IModule
    {
        TModule.RegisterServices(services);
    }

    public static IEndpointRouteBuilder MapModuleEndpoints<TModule>(this IEndpointRouteBuilder app)
        where TModule : IModule
    {
        TModule.MapEndpoints(app);
        return app;
    }
}
