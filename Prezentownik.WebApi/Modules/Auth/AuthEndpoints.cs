using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
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
        var authGroup = app.MapGroup("/auth");

        authGroup.MapPost("/register", Register)
            .Accepts<RegisterRequest>(MediaTypeNames.Application.Json)
            .WithDescription("Register a new user");

        authGroup.MapIdentityApi<AppUser>();

        authGroup.MapGet("/me", GetUserInfo)
            .RequireAuthorization()
            .WithDescription("Get user information");

        authGroup.MapPut("/me", UpdateUserInfo)
            .RequireAuthorization()
            .WithDescription("Update user information");
    }

    private static async Task<IResult> Register(RegisterRequest request, UserManager<AppUser> userManager, IUserStore<AppUser> userStore)
    {
        Log.Information("Registration attempt for email: {Email}", request.Email);

        if (!userManager.SupportsUserEmail)
        {
            Log.Error("User store does not support email");
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        var emailStore = (IUserEmailStore<AppUser>)userStore;
        var email = request.Email;

        if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
        {
            Log.Warning("Invalid email address provided: {Email}", email);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Email", ["Invalid email address."] }
            });
        }

        var user = new AppUser { DisplayName = request.DisplayName };
        await userStore.SetUserNameAsync(user, email, CancellationToken.None);
        await emailStore.SetEmailAsync(user, email, CancellationToken.None);
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            Log.Warning("Registration failed for email {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        }

        Log.Information("User registered successfully: {Email}", email);
        return Results.Ok();
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
