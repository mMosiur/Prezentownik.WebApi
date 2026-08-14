namespace Prezentownik.WebApi.Modules.UserLists;

public abstract class UserListsModule : IModule
{
    public static void RegisterServices(IServiceCollection services)
    {
    }

    public static void MapEndpoints(IEndpointRouteBuilder app)
        => UserListsEndpoints.MapEndpoints(app);
}
