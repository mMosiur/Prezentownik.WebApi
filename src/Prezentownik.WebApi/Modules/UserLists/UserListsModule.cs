namespace Prezentownik.WebApi.Modules.UserLists;

public abstract class UserListsModule : IModule
{
    public static void RegisterModule(IHostApplicationBuilder builder)
    {
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => UserListsEndpoints.MapEndpoints(app);
}
