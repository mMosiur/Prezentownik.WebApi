using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Auth.DTOs;
using Serilog;

namespace Prezentownik.WebApi.Modules.Auth;

public static class AuthEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth")
            .WithTags("Auth");

        authGroup.MapIdentityApi<AppUser>();

        authGroup.MapGet("/me", GetUserInfo)
            .RequireAuthorization()
            .WithDescription("Get user information");

        authGroup.MapPut("/me", UpdateUserInfo)
            .RequireAuthorization()
            .WithDescription("Update user information");
    }

    private static async Task<IResult> GetUserInfo(ClaimsPrincipal user, UserManager<AppUser> userManager)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Log.Information("Getting user info for user: {UserId}", userId);

        if (await userManager.GetUserAsync(user) is not { } appUser)
        {
            Log.Warning("User not found: {UserId}", userId);
            return Results.NotFound();
        }

        Log.Information("User info retrieved successfully for: {Email}", appUser.Email);
        return Results.Ok(new UserInfoResponse(appUser.Email!, appUser.DisplayName));
    }

    private static async Task<IResult> UpdateUserInfo(ClaimsPrincipal user, [FromBody] UpdateUserInfoRequest request, UserManager<AppUser> userManager)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Log.Information("Updating user info for user: {UserId} (New display name: {DisplayName})", userId, request.DisplayName);

        if (await userManager.GetUserAsync(user) is not { } appUser)
        {
            Log.Warning("User not found: {UserId}", userId);
            return Results.NotFound();
        }

        appUser.DisplayName = request.DisplayName;
        await userManager.UpdateAsync(appUser);

        Log.Information("User info updated successfully for: {Email}", appUser.Email);
        return Results.Ok(new UserInfoResponse(appUser.Email!, appUser.DisplayName));
    }
}
