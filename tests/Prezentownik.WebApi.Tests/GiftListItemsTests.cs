using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Models.Enums;
using Xunit;

namespace Prezentownik.WebApi.Tests;

public class GiftListItemsTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreatingGiftList_ShouldGenerateUuidV7AndSave()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Holiday Wishlist", "Gifts for holidays", "user_1");
        context.GiftLists.Add(giftList);
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, giftList.Id);

        using var verifyContext = CreateDbContext(dbName);
        var saved = await verifyContext.GiftLists.FindAsync(giftList.Id);
        Assert.NotNull(saved);
        Assert.Equal("Holiday Wishlist", saved.Name);
    }

    [Fact]
    public async Task AddingItemToTrackedGiftList_ShouldMarkItemAsAddedAndGenerateUuidV7()
    {
        var dbName = Guid.NewGuid().ToString();

        // Arrange
        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        // Simulate fetching list from DB in another context or tracking it as Unchanged
        using var context2 = CreateDbContext(dbName);
        var trackedList = await context2.GiftLists.Include(g => g.Items).FirstAsync(g => g.Id == giftList.Id);

        // Act
        var item = Item.CreateFromRequest("Wireless Headphones", "Sony WH-1000XM5", 1, ItemType.Singular, 1);
        trackedList.Items.Add(item);

        context2.ChangeTracker.DetectChanges();
        var entryBeforeSave = context2.Entry(item);
        Assert.Equal(EntityState.Added, entryBeforeSave.State);

        await context2.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(giftList.Id, item.GiftListId);

        using var context3 = CreateDbContext(dbName);
        var savedList = await context3.GiftLists.Include(g => g.Items).FirstAsync(g => g.Id == giftList.Id);
        Assert.Single(savedList.Items);
        Assert.Equal("Wireless Headphones", savedList.Items.First().Name);
        Assert.Equal(item.Id, savedList.Items.First().Id);
    }

    [Fact]
    public async Task EditingItem_ShouldSaveSuccessfully_AndPreserveOrderNumber()
    {
        var dbName = Guid.NewGuid().ToString();

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        var item = Item.CreateFromRequest("Original Name", "Original Desc", 1, ItemType.Singular, 1);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        using var context2 = CreateDbContext(dbName);
        var itemToEdit = await context2.Items.FirstAsync(i => i.Id == item.Id);
        itemToEdit.UpdateFromRequest("Updated Name", "Updated Desc", ItemType.Limited, 5);
        await context2.SaveChangesAsync();

        using var context3 = CreateDbContext(dbName);
        var reloaded = await context3.Items.FirstAsync(i => i.Id == item.Id);
        Assert.Equal("Updated Name", reloaded.Name);
        Assert.Equal("Updated Desc", reloaded.Description);
        Assert.Equal(1, reloaded.OrderNumber);
        Assert.Equal(ItemType.Limited, reloaded.Type);
        Assert.Equal(5, reloaded.TargetQuantity);
    }

    [Fact]
    public async Task DeletingItem_ShouldRemoveFromDatabase()
    {
        var dbName = Guid.NewGuid().ToString();

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        var item = Item.CreateFromRequest("To Delete", null, 1, ItemType.Singular, 1);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        using var context2 = CreateDbContext(dbName);
        var itemToDelete = await context2.Items.FirstAsync(i => i.Id == item.Id);
        context2.Items.Remove(itemToDelete);
        await context2.SaveChangesAsync();

        using var context3 = CreateDbContext(dbName);
        var remaining = await context3.Items.FirstOrDefaultAsync(i => i.Id == item.Id);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task ClaimingItemOnTrackedGiftList_ShouldMarkClaimAsAddedAndGenerateUuidV7()
    {
        var dbName = Guid.NewGuid().ToString();

        // Arrange
        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        var item = Item.CreateFromRequest("Board Games", null, 1, ItemType.Limited, 3);
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        // Act: Claim item on tracked entity
        using var context2 = CreateDbContext(dbName);
        var trackedList = await context2.GiftLists
            .Include(g => g.Items)
            .ThenInclude(i => i.Claims)
            .FirstAsync(g => g.Id == giftList.Id);

        var trackedItem = trackedList.Items.First();
        trackedItem.AddClaim(2, "Alice");

        context2.ChangeTracker.DetectChanges();
        var claim = trackedItem.Claims.Last();
        var claimEntry = context2.Entry(claim);
        Assert.Equal(EntityState.Added, claimEntry.State);

        await context2.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, claim.Id);
        Assert.NotEqual(Guid.Empty, claim.RevocationToken);

        using var context3 = CreateDbContext(dbName);
        var reloadedItem = await context3.Items.Include(i => i.Claims).FirstAsync(i => i.Id == trackedItem.Id);
        Assert.Single(reloadedItem.Claims);
        Assert.Equal(2, reloadedItem.Claims.First().QuantityClaimed);
        Assert.Equal("Alice", reloadedItem.Claims.First().ClaimerName);
        Assert.Equal(claim.Id, reloadedItem.Claims.First().Id);
    }

    [Fact]
    public async Task RemovingClaim_ShouldRemoveFromItemAndDatabase()
    {
        var dbName = Guid.NewGuid().ToString();

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        var item = Item.CreateFromRequest("Board Games", null, 1, ItemType.Limited, 3);
        item.AddClaim(1, "Bob");
        giftList.Items.Add(item);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        var claim = item.Claims.First();
        var revocationToken = claim.RevocationToken;

        using var context2 = CreateDbContext(dbName);
        var trackedItem = await context2.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        trackedItem.RemoveClaim(revocationToken);
        await context2.SaveChangesAsync();

        using var context3 = CreateDbContext(dbName);
        var reloaded = await context3.Items.Include(i => i.Claims).FirstAsync(i => i.Id == item.Id);
        Assert.Empty(reloaded.Claims);
    }

    [Fact]
    public async Task ReorderingItems_ShouldUpdateOrderNumbersAndPersist()
    {
        var dbName = Guid.NewGuid().ToString();

        using var dbContext = CreateDbContext(dbName);
        var giftList = GiftList.CreateNew("Birthday 2026", "My wishlist", "user123");
        var item1 = Item.CreateFromRequest("Item 1", null, 1, ItemType.Singular, 1);
        var item2 = Item.CreateFromRequest("Item 2", null, 2, ItemType.Singular, 1);
        var item3 = Item.CreateFromRequest("Item 3", null, 3, ItemType.Singular, 1);
        giftList.Items.Add(item1);
        giftList.Items.Add(item2);
        giftList.Items.Add(item3);
        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync();

        // Act: reorder [item3, item1, item2]
        using var context2 = CreateDbContext(dbName);
        var listToReorder = await context2.GiftLists.Include(g => g.Items).FirstAsync(g => g.Id == giftList.Id);
        var itemDict = listToReorder.Items.ToDictionary(i => i.Id);

        var newOrder = new List<Guid> { item3.Id, item1.Id, item2.Id };
        for (var i = 0; i < newOrder.Count; i++)
        {
            itemDict[newOrder[i]].OrderNumber = i + 1;
        }
        await context2.SaveChangesAsync();

        // Assert
        using var context3 = CreateDbContext(dbName);
        var reloaded = await context3.GiftLists.Include(g => g.Items).FirstAsync(g => g.Id == giftList.Id);
        var reloadedItem1 = reloaded.Items.First(i => i.Id == item1.Id);
        var reloadedItem2 = reloaded.Items.First(i => i.Id == item2.Id);
        var reloadedItem3 = reloaded.Items.First(i => i.Id == item3.Id);

        Assert.Equal(2, reloadedItem1.OrderNumber);
        Assert.Equal(3, reloadedItem2.OrderNumber);
        Assert.Equal(1, reloadedItem3.OrderNumber);

        var userListDetails = Prezentownik.WebApi.Modules.UserLists.UserListsMapper.MapToListDetailsDto(reloaded);
        Assert.Equal(item3.Id, userListDetails.Items[0].Id);
        Assert.Equal(item1.Id, userListDetails.Items[1].Id);
        Assert.Equal(item2.Id, userListDetails.Items[2].Id);

        var publicListDto = Prezentownik.WebApi.Modules.Public.PublicMapper.MapToPublicListDto(reloaded, null);
        Assert.Equal(item3.Id, publicListDto.Items[0].Id);
        Assert.Equal(item1.Id, publicListDto.Items[1].Id);
        Assert.Equal(item2.Id, publicListDto.Items[2].Id);
    }
}
