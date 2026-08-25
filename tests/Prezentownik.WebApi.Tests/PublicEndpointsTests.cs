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
        giftList.Items.Add(Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Singular, 1));
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
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
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
        giftList.Items.Add(Item.CreateFromRequest("Stroller", null, 1, Models.Enums.ItemType.Singular, 1));
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
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
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
        Assert.Equal("someone_else", savedItem.Claims[0].ClaimerId);
    }

    [Fact]
    public async Task ClaimGift_WhenListOwnerTriesToClaim_ShouldReturnForbidden()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var request = new CreateClaimRequest(1, "Owner");
        var ownerPrincipal = CreatePrincipal(ownerId);

        var result = await PublicEndpoints.ClaimGift(giftList.Id, item.Id, request, ownerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<ForbidHttpResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task GetList_WhenAuthenticatedUserHasClaimed_ShouldSetIsClaimedByCurrentUserAndIsMyClaim()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";
        var claimantId = "claimant_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId, "Owner Name"));
        dbContext.Users.Add(new AppUser { Id = claimantId, UserName = claimantId, DisplayName = "Claimant Name" });
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Singular, 1);
        item.AddClaim(1, null, claimantId);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var claimantPrincipal = CreatePrincipal(claimantId);
        var result = await PublicEndpoints.GetList(giftList.Id, claimantPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<PublicListDto>>(result, exactMatch: false);
        var retrievedItem = okResult.Value!.Items[0];
        Assert.True(retrievedItem.IsClaimedByCurrentUser);
        Assert.Single(retrievedItem.Claims);
        Assert.True(retrievedItem.Claims[0].IsMyClaim);
        Assert.NotNull(retrievedItem.Claims[0].RevocationToken);
        Assert.Equal("Claimant Name", retrievedItem.Claims[0].ClaimerName);
    }

    [Fact]
    public async Task UnclaimGift_WhenAuthenticatedUserUnclaimsWithoutToken_ShouldRemoveTheirClaim()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";
        var claimantId = "claimant_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        dbContext.Users.Add(new AppUser { Id = claimantId, UserName = claimantId });
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
        item.AddClaim(1, "Claimant", claimantId);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var claimantPrincipal = CreatePrincipal(claimantId);
        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, revocationToken: null, claimantPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<NoContent>(result, exactMatch: false);

        using var verifyContext = CreateDbContext(dbName);
        var savedItem = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        Assert.Empty(savedItem.Claims);
    }

    [Fact]
    public async Task UnclaimGift_WhenAuthenticatedUserTriesToUnclaimAnotherUsersClaimWithToken_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";
        var claimantId = "claimant_1";
        var otherUserId = "other_user";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        dbContext.Users.Add(new AppUser { Id = claimantId, UserName = claimantId });
        dbContext.Users.Add(new AppUser { Id = otherUserId, UserName = otherUserId });
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
        item.AddClaim(1, "Claimant", claimantId);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var token = item.Claims[0].RevocationToken;
        var otherPrincipal = CreatePrincipal(otherUserId);
        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, token, otherPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task UnclaimGift_WhenAnonymousUserProvidesNoToken_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Singular, 1);
        item.AddClaim(1, "Anonymous");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var anonymousPrincipal = CreatePrincipal(null);
        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, revocationToken: null, anonymousPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task UnclaimGift_WhenListOwnerTriesToUnclaim_ShouldReturnForbidden()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Singular, 1);
        item.AddClaim(1, "Uncle Bob");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var revocationToken = item.Claims[0].RevocationToken;
        var ownerPrincipal = CreatePrincipal(ownerId);

        var result = await PublicEndpoints.UnclaimGift(giftList.Id, item.Id, revocationToken, ownerPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<ForbidHttpResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task UnclaimGift_WithValidToken_ShouldRemoveClaim()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        var giftList = GiftList.CreateNew("Birthday Wishlist", null, ownerId);
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
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
        var item = Item.CreateFromRequest("Headphones", null, 1, Models.Enums.ItemType.Limited, 2);
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

    [Fact]
    public async Task AdoptClaims_WhenValidTokensProvided_ShouldAssignClaimsToCurrentUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var owner1Id = "owner_1";
        var owner2Id = "owner_2";
        var userId = "new_user";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(owner1Id));
        dbContext.Users.Add(CreateOwnerUser(owner2Id));
        dbContext.Users.Add(new AppUser { Id = userId, UserName = userId });

        var list1 = GiftList.CreateNew("List 1", null, owner1Id);
        var item1 = Item.CreateFromRequest("Item 1", null, 1, Models.Enums.ItemType.Singular, 1);
        var item2 = Item.CreateFromRequest("Item 2", null, 2, Models.Enums.ItemType.Singular, 1);
        item1.AddClaim(1, "Claimer 1");
        item2.AddClaim(1, "Claimer 2");
        list1.Items.Add(item1);
        list1.Items.Add(item2);

        var list2 = GiftList.CreateNew("List 2", null, owner2Id);
        var item3 = Item.CreateFromRequest("Item 3", null, 1, Models.Enums.ItemType.Singular, 1);
        item3.AddClaim(1, "Claimer 3");
        list2.Items.Add(item3);

        dbContext.GiftLists.AddRange(list1, list2);
        await dbContext.SaveChangesAsync();

        var token1 = item1.Claims[0].RevocationToken;
        var token2 = item2.Claims[0].RevocationToken;
        var token3 = item3.Claims[0].RevocationToken;

        var request = new AdoptClaimsRequest([token1, token2, token3]);
        var userPrincipal = CreatePrincipal(userId);

        var result = await PublicEndpoints.AdoptClaims(request, userPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AdoptClaimsResponse>>(result, exactMatch: false);
        Assert.Equal(3, okResult.Value!.AdoptedClaimsCount);

        using var verifyContext = CreateDbContext(dbName);
        var claims = await verifyContext.GiftClaims.ToListAsync();
        Assert.Equal(3, claims.Count);
        Assert.All(claims, c => Assert.Equal(userId, c.ClaimerId));
    }

    [Fact]
    public async Task AdoptClaims_WhenClaimBelongsToAnotherUser_ShouldNotOverwriteClaimerId()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";
        var existingClaimantId = "existing_user";
        var newUserId = "new_user";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));
        dbContext.Users.Add(new AppUser { Id = existingClaimantId, UserName = existingClaimantId });
        dbContext.Users.Add(new AppUser { Id = newUserId, UserName = newUserId });

        var list = GiftList.CreateNew("List", null, ownerId);
        var item1 = Item.CreateFromRequest("Item 1", null, 1, Models.Enums.ItemType.Singular, 1);
        var item2 = Item.CreateFromRequest("Item 2", null, 2, Models.Enums.ItemType.Singular, 1);
        item1.AddClaim(1, "Existing", existingClaimantId);
        item2.AddClaim(1, "Anonymous");
        list.Items.Add(item1);
        list.Items.Add(item2);

        dbContext.GiftLists.Add(list);
        await dbContext.SaveChangesAsync();

        var token1 = item1.Claims[0].RevocationToken;
        var token2 = item2.Claims[0].RevocationToken;

        var request = new AdoptClaimsRequest([token1, token2]);
        var userPrincipal = CreatePrincipal(newUserId);

        var result = await PublicEndpoints.AdoptClaims(request, userPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AdoptClaimsResponse>>(result, exactMatch: false);
        Assert.Equal(1, okResult.Value!.AdoptedClaimsCount);

        using var verifyContext = CreateDbContext(dbName);
        var savedItem1 = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item1.Id);
        var savedItem2 = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item2.Id);

        Assert.Equal(existingClaimantId, savedItem1.Claims[0].ClaimerId);
        Assert.Equal(newUserId, savedItem2.Claims[0].ClaimerId);
    }

    [Fact]
    public async Task AdoptClaims_WhenUserIsListOwner_ShouldSkipAdoption()
    {
        var dbName = Guid.NewGuid().ToString();
        var ownerId = "owner_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(CreateOwnerUser(ownerId));

        var list = GiftList.CreateNew("List", null, ownerId);
        var item = Item.CreateFromRequest("Item 1", null, 1, Models.Enums.ItemType.Singular, 1);
        item.AddClaim(1, "Anonymous");
        list.Items.Add(item);

        dbContext.GiftLists.Add(list);
        await dbContext.SaveChangesAsync();

        var token = item.Claims[0].RevocationToken;
        var request = new AdoptClaimsRequest([token]);
        var ownerPrincipal = CreatePrincipal(ownerId);

        var result = await PublicEndpoints.AdoptClaims(request, ownerPrincipal, dbContext, CancellationToken.None);

        var okResult = Assert.IsType<Ok<AdoptClaimsResponse>>(result, exactMatch: false);
        Assert.Equal(0, okResult.Value!.AdoptedClaimsCount);

        using var verifyContext = CreateDbContext(dbName);
        var savedItem = await verifyContext.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        Assert.Null(savedItem.Claims[0].ClaimerId);
    }

    [Fact]
    public async Task AdoptClaims_WhenTokensDoNotExistOrEmpty_ShouldReturnZeroCount()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_1";

        await using var dbContext = CreateDbContext(dbName);
        dbContext.Users.Add(new AppUser { Id = userId, UserName = userId });
        await dbContext.SaveChangesAsync();

        var userPrincipal = CreatePrincipal(userId);

        var emptyResult = await PublicEndpoints.AdoptClaims(new AdoptClaimsRequest([]), userPrincipal, dbContext, CancellationToken.None);
        var emptyOk = Assert.IsType<Ok<AdoptClaimsResponse>>(emptyResult, exactMatch: false);
        Assert.Equal(0, emptyOk.Value!.AdoptedClaimsCount);

        var nonExistentResult = await PublicEndpoints.AdoptClaims(new AdoptClaimsRequest([Guid.NewGuid(), Guid.NewGuid()]), userPrincipal, dbContext, CancellationToken.None);
        var nonExistentOk = Assert.IsType<Ok<AdoptClaimsResponse>>(nonExistentResult, exactMatch: false);
        Assert.Equal(0, nonExistentOk.Value!.AdoptedClaimsCount);
    }

    [Fact]
    public async Task AdoptClaims_WhenAnonymousUser_ShouldReturnUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateDbContext(dbName);
        var anonymousPrincipal = CreatePrincipal(null);

        var result = await PublicEndpoints.AdoptClaims(new AdoptClaimsRequest([Guid.NewGuid()]), anonymousPrincipal, dbContext, CancellationToken.None);

        Assert.IsType<UnauthorizedHttpResult>(result, exactMatch: false);
    }
}
