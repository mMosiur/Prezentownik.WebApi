using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Modules.Public.DTOs;

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
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        // If the list is owner by current user, redirect to logged-in user list get endpoint
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == giftList.OwnerId) return Results.Redirect($"user/lists/{listId}");

        PublicListDto response = PublicMapper.MapToPublicListDto(giftList);

        return Results.Ok(response);
    }

    private static async Task<IResult> ClaimGift(Guid listId, Guid itemId, CreateClaimRequest request,
        ClaimsPrincipal principal, IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
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
        var dbContext = sp.GetRequiredService<AppDbContext>();

        var giftList = await dbContext.GiftLists
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        var item = giftList.Items
            .FirstOrDefault(i => i.Id == itemId);

        if (item is null) return Results.NotFound();

        var claimsToRevoke = item.Claims
            .Where(c => c.RevocationToken == revocationToken)
            .ToList();

        dbContext.GiftClaims.RemoveRange(claimsToRevoke);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
