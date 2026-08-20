using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Public;
using Prezentownik.WebApi.Modules.Public.DTOs;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class PublicEndpointsTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreatePrincipal(string? userId)
    {
        if (userId is null) return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static AppUser CreateOwnerUser(string userId, string? displayName = null)
        => new() { Id = userId, UserName = userId, DisplayName = displayName };

    [Fact]
    public async Task GetList_WhenRequestedByOwner_ShouldReturnForbidden()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        giftList.Items.Add(Item.CreateFromRequest("Headphones", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1));
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var ownerPrincipal = CreatePrincipal(ownerId);
        var result = await PublicEndpoints.GetList(giftList.Id, ownerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<ForbidHttpResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task GetList_WhenRequestedByStranger_ShouldReturnOkWithClaims()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", "Please and thank you", ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Limited, 2);
        item.AddClaim(1, "Aunt Mary");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var strangerPrincipal = CreatePrincipal("someone_else");
        var result = await PublicEndpoints.GetList(giftList.Id, strangerPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<PublicListDto>>(result, exactMatch: false);
        Assert.Equal("Birthday Wishlist", okResult.Value!.Name);
        Assert.Single(okResult.Value.Items);
        Assert.Single(okResult.Value.Items[0].Claims);
        Assert.Equal("Aunt Mary", okResult.Value.Items[0].Claims[0].ClaimerName);
    }

    [Fact]
    public async Task GetList_WhenRequestedByAnonymousVisitor_ShouldReturnOkWithClaims()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Baby Shower", null, ownerId);
        giftList.Items.Add(Item.CreateFromRequest("Stroller", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1));
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var anonymousPrincipal = CreatePrincipal(null);
        var result = await PublicEndpoints.GetList(giftList.Id, anonymousPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<Ok<PublicListDto>>(result, exactMatch: false);
    }

    [Fact]
    public async Task GetList_WhenListDoesNotExist_ShouldReturnNotFound()
    {
        var dbName = Guid.NewGuid().ToString();

        await using var dbContext = CreateDbContext(dbName);
        var strangerPrincipal = CreatePrincipal("someone_else");

        var result = await PublicEndpoints.GetList(Guid.NewGuid(), strangerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<NotFound>(result, exactMatch: false);
    }

    [Fact]
    public async Task ClaimGift_WhenValid_ShouldAddClaimAndReturnRevocationToken()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Limited, 2);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var request = new CreateClaimRequest(1, "Uncle Bob");
        var strangerPrincipal = CreatePrincipal("someone_else");

        var result = await PublicEndpoints.ClaimGift(giftList.Id, item.Id, request, strangerPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<CreateClaimResponse>>(result, exactMatch: false);
        Assert.NotEqual(Guid.Empty, okResult.Value!.RevocationToken);

        using var verifyContext = CreateDbContext(dbName);
        var savedItem = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        Assert.Single(savedItem.Claims);
        Assert.Equal("Uncle Bob", savedItem.Claims[0].ClaimerName);
    }

    [Fact]
    public async Task UnclaimGift_WithValidToken_ShouldRemoveClaim()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Limited, 2);
        item.AddClaim(1, "Uncle Bob");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var revocationToken = item.Claims[0].RevocationToken;
        var strangerPrincipal = CreatePrincipal("someone_else");

        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, revocationToken, strangerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<NoContent>(result, exactMatch: false);

        using var verifyContext = CreateDbContext(dbName);
        var savedItem = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        Assert.Empty(savedItem.Claims);
    }

    [Fact]
    public async Task UnclaimGift_WithInvalidToken_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Limited, 2);
        item.AddClaim(1, "Uncle Bob");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var strangerPrincipal = CreatePrincipal("someone_else");

        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, Guid.NewGuid(), strangerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }
}
