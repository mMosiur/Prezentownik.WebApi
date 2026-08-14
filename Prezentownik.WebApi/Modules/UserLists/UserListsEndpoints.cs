using System.Net.Mime;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Extensions;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.UserLists.DTOs;

namespace Prezentownik.WebApi.Modules.UserLists;

public static class UserListsEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user/lists")
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

    private static async Task<IResult> GetAllLists(
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var userId = principal.GetUserId()!;

        var giftLists = await dbContext.GiftLists
            .Where(l => l.OwnerId == userId)
            .ToListAsync(cancellationToken);

        List<ListSummaryDto> response = giftLists
            .Select(UserListsMapper.MapToListSummaryDto)
            .ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateNewList(CreateGiftListRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var userId = principal.GetUserId()!;

        var giftList = GiftList.CreateNew(
            name: request.Name,
            description: request.Description,
            ownerId: userId);

        dbContext.GiftLists.Add(giftList);
        await dbContext.SaveChangesAsync(cancellationToken);

        ListSummaryDto response = UserListsMapper.MapToListSummaryDto(giftList);

        return Results.CreatedAtRoute(nameof(GetListDetails), new { giftList.Id }, response);
    }

    private static async Task<IResult> GetListDetails(Guid listId,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, includeItems: true, cancellationToken);

        if (giftList is null) return Results.NotFound();

        return Results.Ok(UserListsMapper.MapToListDetailsDto(giftList));
    }

    private static async Task<IResult> EditList(Guid listId, UpdateGiftListRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        giftList.Name = request.Name;
        giftList.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToListSummaryDto(giftList));
    }

    private static async Task<IResult> DeleteList(Guid listId,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        dbContext.GiftLists.Remove(giftList);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> AddListItem(Guid listId, UpsertItemRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var giftList = await GetGiftListWithOwnerAsync(dbContext, listId, userId, includeItems: true, ct: cancellationToken);

        if (giftList is null) return Results.NotFound();

        var item = Item.CreateFromRequest(
            request.Name,
            request.Description,
            request.OrderNumber,
            UserListsMapper.MapItemTypeDomain(request.Type),
            request.TargetQuantity);

        giftList.Items.Add(item);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToItemDto(item));
    }

    private static async Task<IResult> EditListItem(Guid listId, Guid itemId, UpsertItemRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var item = await dbContext.Items
            .Include(i => i.GiftList)
            .Where(i => i.Id == itemId && i.GiftList.OwnerId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null) return Results.NotFound();

        UserListsMapper.MapOntoItem(item, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(UserListsMapper.MapToItemDto(item));
    }

    private static async Task<IResult> DeleteListItem(Guid listId, Guid itemId,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var userId = principal.GetUserId()!;

        var item = await dbContext.Items
            .Include(i => i.GiftList)
            .Where(i => i.Id == itemId && i.GiftList.OwnerId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null) return Results.NotFound();

        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
