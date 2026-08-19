using System.Net.Mime;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Extensions;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.UserLists.DTOs;
using Serilog;

namespace Prezentownik.WebApi.Modules.UserLists;

public static class UserListsEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user/lists")
            .WithTags("UserLists")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/", GetAllLists)
            .Produces<List<ListSummaryDto>>(StatusCodes.Status200OK)
            .WithDescription("Get all user's lists");

        group.MapPost("/", CreateNewList)
            .Accepts<CreateGiftListRequest>(MediaTypeNames.Application.Json)
            .Produces<ListSummaryDto>(StatusCodes.Status201Created)
            .WithDescription("Create a new list");

        group.MapGet("/{listId:guid}", GetListDetails)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ListDetailsDto>(StatusCodes.Status200OK)
            .WithName(nameof(GetListDetails))
            .WithDescription("Edit page view");

        group.MapPut("/{listId:guid}", EditList)
            .Accepts<UpdateGiftListRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ListSummaryDto>(StatusCodes.Status200OK)
            .WithDescription("Update list info");

        group.MapDelete("/{listId:guid}", DeleteList)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a list");

        group.MapPost("/{listId:guid}/items", AddListItem)
            .Accepts<UpsertItemRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .WithDescription("Add a gift item");

        group.MapPut("/{listId:guid}/items/{itemId:guid}", EditListItem)
            .Accepts<UpsertItemRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .WithDescription("Edit a gift item");

        group.MapPut("/{listId:guid}/items/reorder", ReorderListItems)
            .Accepts<ReorderItemsRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces<ListDetailsDto>(StatusCodes.Status200OK)
            .WithDescription("Reorder list items");

        group.MapDelete("/{listId:guid}/items/{itemId:guid}", DeleteListItem)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Remove a gift item");
    }

    private static async Task<GiftList?> GetGiftListWithOwnerAsync(AppDbContext dbContext, Guid listId, string userId, bool includeItems = false, CancellationToken ct = default)
    {
        var query = dbContext.GiftLists.AsQueryable();
        if (includeItems) query = query.Include(gl => gl.Items);
        return await query.FirstOrDefaultAsync(gl => gl.Id == listId && gl.OwnerId == userId, ct);
    }

    private static async Task<Item?> GetItemWithOwnerAsync(AppDbContext dbContext, Guid itemId, string userId, bool includeClaims = false, CancellationToken ct = default)
    {
        var query = dbContext.Items.AsQueryable();
        if (includeClaims) query = query.Include(i => i.Claims);
        return await query.FirstOrDefaultAsync(i => i.Id == itemId && i.GiftList.OwnerId == userId, ct);
    }

    internal static async Task<IResult> GetAllLists(
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Retrieving all gift lists for user {UserId}", userId);

        var giftLists = await dbContext.GiftLists
            .Where(l => l.OwnerId == userId)
            .ToListAsync(cancellationToken);

        List<ListSummaryDto> response = giftLists
            .Select(UserListsMapper.MapToListSummaryDto)
            .ToList();

        return Results.Ok(response);
    }

    internal static async Task<IResult> CreateNewList(CreateGiftListRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Creating new gift list {ListName} for user {UserId}", request.Name, userId);

        var giftList = GiftList.CreateNew(
            name: request.Name,
            description: request.Description,
            ownerId: userId);

        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync(cancellationToken);

        ListSummaryDto response = UserListsMapper.MapToListSummaryDto(giftList);

        return Results.CreatedAtRoute(nameof(GetListDetails), new { listId = giftList.Id }, response);
    }

    internal static async Task<IResult> GetListDetails(Guid listId,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Debug("Retrieving details for gift list {ListId} (User: {UserId})", listId, userId);

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, includeItems: true, cancellationToken);

        if (giftList is null) return Results.NotFound();

        return Results.Ok(UserListsMapper.MapToListDetailsDto(giftList));
    }

    internal static async Task<IResult> EditList(Guid listId, UpdateGiftListRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Updating gift list {ListId} for user {UserId}", listId, userId);

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        giftList.Name = request.Name;
        giftList.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToListSummaryDto(giftList));
    }

    internal static async Task<IResult> DeleteList(Guid listId,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, ct: cancellationToken);

        if (giftList is null)
        {
            Log.Warning("Attempted to delete gift list {ListId} but it was not found for user {UserId}", listId, userId);
            return Results.NotFound();
        }

        dbContext.GiftLists.Remove(giftList);
        await dbContext.SaveChangesAsync(cancellationToken);

        Log.Information("Deleted gift list {ListId} for user {UserId}", listId, userId);

        return Results.NoContent();
    }

    internal static async Task<IResult> AddListItem(Guid listId, UpsertItemRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Adding new item {ItemName} to list {ListId} for user {UserId}", request.Name, listId, userId);

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, includeItems: true, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        var nextOrder = giftList.Items.Count > 0
            ? giftList.Items.Max(i => i.OrderNumber) + 1
            : 1;

        var item = Item.CreateFromRequest(
            request.Name,
            request.Description,
            nextOrder,
            UserListsMapper.MapItemTypeDomain(request.Type),
            request.TargetQuantity);

        giftList.Items.Add(item);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToItemDto(item));
    }

    internal static async Task<IResult> EditListItem(Guid listId, Guid itemId, UpsertItemRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Editing item {ItemId} in list {ListId} for user {UserId}", itemId, listId, userId);

        var item = await GetItemWithOwnerAsync(dbContext, itemId, userId, includeClaims: true, ct: cancellationToken);

        if (item is null || item.GiftListId != listId) return Results.NotFound();

        UserListsMapper.MapOntoItem(item, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToItemDto(item));
    }

    internal static async Task<IResult> ReorderListItems(Guid listId, ReorderItemsRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        Log.Information("Reordering items for gift list {ListId} for user {UserId}", listId, userId);

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, includeItems: true, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        if (request.ItemIds is null)
        {
            return Results.BadRequest(new { message = "Item IDs list is required." });
        }

        if (request.ItemIds.Count != giftList.Items.Count)
        {
            return Results.BadRequest(new { message = "The number of item IDs does not match the number of items in the list." });
        }

        if (request.ItemIds.Distinct().Count() != request.ItemIds.Count)
        {
            return Results.BadRequest(new { message = "Duplicate item IDs provided." });
        }

        var itemDict = giftList.Items.ToDictionary(i => i.Id);
        foreach (var itemId in request.ItemIds)
        {
            if (!itemDict.ContainsKey(itemId))
            {
                return Results.BadRequest(new { message = $"Item with ID {itemId} does not belong to this gift list." });
            }
        }

        for (var i = 0; i < request.ItemIds.Count; i++)
        {
            itemDict[request.ItemIds[i]].OrderNumber = i + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToListDetailsDto(giftList));
    }

    internal static async Task<IResult> DeleteListItem(Guid listId, Guid itemId,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId()!;

        var item = await GetItemWithOwnerAsync(dbContext, itemId, userId, ct: cancellationToken);

        if (item is null)
        {
            Log.Warning("Attempted to delete item {ItemId} but it was not found for user {UserId}", itemId, userId);
            return Results.NotFound();
        }

        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        Log.Information("Deleted item {ItemId} from list {ListId} for user {UserId}", itemId, listId, userId);

        return Results.NoContent();
    }
}
