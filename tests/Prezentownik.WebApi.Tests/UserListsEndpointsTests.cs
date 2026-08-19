using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Models.Enums;
using Prezentownik.WebApi.Modules.UserLists;
using Prezentownik.WebApi.Modules.UserLists.DTOs;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class UserListsEndpointsTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task AddListItem_ShouldAutoAssignIncrementingOrderNumber()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        // Add 1st item
        var item1Req = new CreateItemRequest("First Item", "Desc 1", Modules.UserLists.DTOs.ItemType.Singular, 1);
        var result1 = await UserListsEndpoints.AddListItem(giftList.Id, item1Req, principal, dbContext, CancellationToken.None);
        var okResult1 = Assert.IsAssignableFrom<IValueHttpResult<ItemDto>>(result1);
        Assert.Equal(1, okResult1.Value!.OrderNumber);

        // Add 2nd item
        var item2Req = new CreateItemRequest("Second Item", "Desc 2", Modules.UserLists.DTOs.ItemType.Limited, 3);
        var result2 = await UserListsEndpoints.AddListItem(giftList.Id, item2Req, principal, dbContext, CancellationToken.None);
        var okResult2 = Assert.IsAssignableFrom<IValueHttpResult<ItemDto>>(result2);
        Assert.Equal(2, okResult2.Value!.OrderNumber);
    }

    [Fact]
    public async Task EditListItem_ShouldUpdateProperties_WithoutModifyingOrderNumber()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        var item = Item.CreateFromRequest("Old Name", "Old Desc", 5, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var updateReq = new UpdateItemRequest("New Name", "New Desc", Modules.UserLists.DTOs.ItemType.Limited, 10);
        var result = await UserListsEndpoints.EditListItem(giftList.Id, item.Id, updateReq, principal, dbContext, CancellationToken.None);
        var okResult = Assert.IsAssignableFrom<IValueHttpResult<ItemDto>>(result);

        Assert.Equal("New Name", okResult.Value!.Name);
        Assert.Equal("New Desc", okResult.Value!.Description);
        Assert.Equal(5, okResult.Value!.OrderNumber);
        Assert.Equal(10, okResult.Value!.TargetQuantity);
    }

    [Fact]
    public async Task ReorderListItems_WhenValid_ShouldReorderAndReturnUpdatedList()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        var itemA = Item.CreateFromRequest("Item A", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        var itemB = Item.CreateFromRequest("Item B", null, 2, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        var itemC = Item.CreateFromRequest("Item C", null, 3, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(itemA);
        giftList.Items.Add(itemB);
        giftList.Items.Add(itemC);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        // Reorder: C, A, B
        var reorderReq = new ReorderItemsRequest([itemC.Id, itemA.Id, itemB.Id]);
        var result = await UserListsEndpoints.ReorderListItems(giftList.Id, reorderReq, principal, dbContext, CancellationToken.None);
        var okResult = Assert.IsAssignableFrom<IValueHttpResult<ListDetailsDto>>(result);

        Assert.Equal(itemC.Id, okResult.Value!.Items[0].Id);
        Assert.Equal(1, okResult.Value!.Items[0].OrderNumber);

        Assert.Equal(itemA.Id, okResult.Value!.Items[1].Id);
        Assert.Equal(2, okResult.Value!.Items[1].OrderNumber);

        Assert.Equal(itemB.Id, okResult.Value!.Items[2].Id);
        Assert.Equal(3, okResult.Value!.Items[2].OrderNumber);
    }

    [Fact]
    public async Task ReorderListItems_WhenListNotFound_ShouldReturnNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var reorderReq = new ReorderItemsRequest([Guid.NewGuid()]);
        var result = await UserListsEndpoints.ReorderListItems(Guid.NewGuid(), reorderReq, principal, dbContext, CancellationToken.None);

        Assert.IsAssignableFrom<NotFound>(result);
    }

    [Fact]
    public async Task ReorderListItems_WhenCountMismatched_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        var itemA = Item.CreateFromRequest("Item A", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(itemA);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var reorderReq = new ReorderItemsRequest([]); // Empty list when list has 1 item
        var result = await UserListsEndpoints.ReorderListItems(giftList.Id, reorderReq, principal, dbContext, CancellationToken.None);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReorderListItems_WhenDuplicateIds_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        var itemA = Item.CreateFromRequest("Item A", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        var itemB = Item.CreateFromRequest("Item B", null, 2, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(itemA);
        giftList.Items.Add(itemB);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var reorderReq = new ReorderItemsRequest([itemA.Id, itemA.Id]);
        var result = await UserListsEndpoints.ReorderListItems(giftList.Id, reorderReq, principal, dbContext, CancellationToken.None);

        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task ReorderListItems_WhenItemIdNotInList_ShouldReturnBadRequest()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = "user_42";
        var principal = CreatePrincipal(userId);

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Wishlist", null, userId);
        var itemA = Item.CreateFromRequest("Item A", null, 1, Prezentownik.WebApi.Models.Enums.ItemType.Singular, 1);
        giftList.Items.Add(itemA);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var reorderReq = new ReorderItemsRequest([Guid.NewGuid()]);
        var result = await UserListsEndpoints.ReorderListItems(giftList.Id, reorderReq, principal, dbContext, CancellationToken.None);

        var statusResult = (IStatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }
}
