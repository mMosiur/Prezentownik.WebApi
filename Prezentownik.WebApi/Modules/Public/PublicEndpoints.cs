using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Modules.Public.DTOs;
using Serilog;

namespace Prezentownik.WebApi.Modules.Public;

public static class PublicEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/lists")
            .ProducesValidationProblem();

        group.MapGet("/{listId:guid}", GetList)
            .Produces<PublicListDto>(StatusCodes.Status200OK)
            .WithDescription("View list & items");

        group.MapPost("/{listId:guid}/items/{itemId:guid}/claims", ClaimGift)
            .Accepts<CreateClaimRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateClaimResponse>(StatusCodes.Status200OK)
            .WithDescription("Claim a gift");

        group.MapDelete("/{listId:guid}/items/{itemId:guid}/claims", UnclaimGift)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .WithDescription("Unclaim a gift");
    }

    private static async Task<IResult> GetList(Guid listId,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        Log.Information("Getting public list {ListId}", listId);

        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        // If the list is owner by current user, redirect to logged-in user list get endpoint
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == giftList.OwnerId) return Results.Redirect($"user/lists/{listId}");

        PublicListDto response = PublicMapper.MapToPublicListDto(giftList, giftList.Owner);

        return Results.Ok(response);
    }

    private static async Task<IResult> ClaimGift(Guid listId, Guid itemId, CreateClaimRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        Log.Information("Claiming gift {ItemId} from public list {ListId} (Claimer name: {ClaimerName}, quantity: {ClaimQuantity})", itemId, listId, request.ClaimerName, request.QuantityClaimed);

        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        var item = giftList.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return Results.NotFound();

        try
        {
            item.AddClaim(request.QuantityClaimed, request.ClaimerName);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newClaim = item.Claims.Last();
        CreateClaimResponse response = new(newClaim.RevocationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> UnclaimGift(Guid listId, Guid itemId, [FromQuery] Guid? revocationToken,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        Log.Information("Unclaiming gift {ItemId} from public list {ListId} (Revocation token: {RevocationToken})", itemId, listId, revocationToken);

        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        var item = giftList.Items
            .FirstOrDefault(i => i.Id == itemId);

        if (item is null) return Results.NotFound();
        if (revocationToken is null) return Results.BadRequest(new { message = "Revocation token is required." });

        try
        {
            item.RemoveClaim(revocationToken.Value);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
