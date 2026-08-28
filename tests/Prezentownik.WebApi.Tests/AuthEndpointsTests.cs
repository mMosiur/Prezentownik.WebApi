using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Auth;
using Prezentownik.WebApi.Modules.Auth.DTOs;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class AuthEndpointsTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    private static UserManager<AppUser> CreateUserManager(AppDbContext dbContext)
    {
        var userStore = new UserStore<AppUser>(dbContext);
        return new UserManager<AppUser>(
            userStore,
            null!,
            new PasswordHasher<AppUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, string? email = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task GetUserInfo_WhenUserExists_ReturnsOkWithUserInfo()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_123";
        var principal = CreatePrincipal(userId);

        await using var dbContext = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        using var userManager = CreateUserManager(dbContext);
        var result = await AuthEndpoints.GetUserInfo(principal, userManager);

        var okResult = Assert.IsType<Ok<UserInfoResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("test@example.com", okResult.Value.Email);
        Assert.Equal("Test User", okResult.Value.DisplayName);
    }

    [Fact]
    public async Task GetUserInfo_WhenUserNotFound_ReturnsNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var principal = CreatePrincipal("non_existent_user");

        await using var dbContext = CreateDbContext(dbName);
        using var userManager = CreateUserManager(dbContext);

        var result = await AuthEndpoints.GetUserInfo(principal, userManager);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task UpdateUserInfo_WhenUserExists_UpdatesDisplayNameAndReturnsOk()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_456";
        var principal = CreatePrincipal(userId);

        await using var dbContext = CreateDbContext(dbName);
        var user = new AppUser
        {
            Id = userId,
            UserName = "user456@example.com",
            Email = "user456@example.com",
            DisplayName = "Old Name"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        using var userManager = CreateUserManager(dbContext);
        var request = new UpdateUserInfoRequest("New Display Name");
        var result = await AuthEndpoints.UpdateUserInfo(principal, request, userManager);

        var okResult = Assert.IsType<Ok<UserInfoResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("user456@example.com", okResult.Value.Email);
        Assert.Equal("New Display Name", okResult.Value.DisplayName);

        // Verify persisted change in db
        var updatedUser = await dbContext.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal("New Display Name", updatedUser.DisplayName);
    }

    [Fact]
    public async Task UpdateUserInfo_WhenUserNotFound_ReturnsNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var principal = CreatePrincipal("non_existent_user");

        await using var dbContext = CreateDbContext(dbName);
        using var userManager = CreateUserManager(dbContext);

        var request = new UpdateUserInfoRequest("New Display Name");
        var result = await AuthEndpoints.UpdateUserInfo(principal, request, userManager);

        Assert.IsType<NotFound>(result);
    }
}
