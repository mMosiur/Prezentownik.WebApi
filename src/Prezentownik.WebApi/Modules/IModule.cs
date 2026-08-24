namespace Prezentownik.WebApi.Modules;

public interface IModule
{
    public static abstract void RegisterModule(IHostApplicationBuilder builder);
    public static abstract void MapEndpoints(IEndpointRouteBuilder app);
}

public static class ModuleExtensions
{
    public static IHostApplicationBuilder RegisterModule<TModule>(this IHostApplicationBuilder builder)
        where TModule : IModule
    {
        TModule.RegisterModule(builder);
        return builder;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints<TModule>(this IEndpointRouteBuilder app)
        where TModule : IModule
    {
        TModule.MapEndpoints(app);
        return app;
    }
}
