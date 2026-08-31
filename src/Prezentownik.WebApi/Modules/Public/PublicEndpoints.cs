using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prezentownik.WebApi.Data;
using Prezentownik.WebApi.Extensions;
using Prezentownik.WebApi.Modules.Commons.DTOs;
using Prezentownik.WebApi.Modules.Public.DTOs;
using Serilog;

namespace Prezentownik.WebApi.Modules.Public;

public static class PublicEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/lists")
            .WithTags("Public")
            .ProducesValidationProblem();

        group.MapGet("/{listId:guid}", GetList)
            .Produces<PublicListDto>()
            .WithDescription("View list & items");

        group.MapPost("/{listId:guid}/items/{itemId:guid}/claims", ClaimGift)
            .Accepts<CreateClaimRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateClaimResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireRateLimiting("public-claims")
            .WithDescription("Claim a gift");

        group.MapDelete("/{listId:guid}/items/{itemId:guid}/claims", UnclaimGift)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireRateLimiting("public-claims")
            .WithDescription("Unclaim a gift");

        group.MapPost("/claims/adopt", AdoptClaims)
            .RequireAuthorization() // Adopting claims requires authentication
            .Accepts<AdoptClaimsRequest>(MediaTypeNames.Application.Json)
            .Produces<AdoptClaimsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .WithDescription("Adopt unauthenticated claims by revocation tokens for the current user");
    }

    internal static async Task<IResult> GetList(Guid listId,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Log.Information("Getting public list {ListId}", listId);

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .ThenInclude(c => c.Claimant)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        // If the list is owned by current user, redirect to logged-in user list get endpoint
        var userId = principal.GetUserId();
        if (userId is not null && userId == giftList.OwnerId)
        {
            return Results.Forbid();
        }

        PublicListDto response = PublicMapper.MapToPublicListDto(giftList, giftList.Owner, userId);

        return Results.Ok(response);
    }

    internal static async Task<IResult> ClaimGift(Guid listId, Guid itemId, CreateClaimRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();

        Log.Information("Claiming gift {ItemId} from public list {ListId} (UserId: {UserId}, Claimant name: {ClaimantName}, quantity: {ClaimQuantity})",
            itemId, listId, userId, request.ClaimantName, request.QuantityClaimed);

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        if (userId is not null && userId == giftList.OwnerId)
        {
            return Results.Forbid();
        }

        var item = giftList.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return Results.NotFound();

        try
        {
            var claimantName = string.IsNullOrWhiteSpace(request.ClaimantName) ? null : request.ClaimantName.Trim();
            item.AddClaim(request.QuantityClaimed, claimantName, userId);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorMessage(ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newClaim = item.Claims.Last();
        CreateClaimResponse response = new(newClaim.RevocationToken);

        return Results.Ok(response);
    }

    internal static async Task<IResult> UnclaimGift(Guid listId, Guid itemId, [FromQuery] Guid? revocationToken,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();

        Log.Information("Unclaiming gift {ItemId} from public list {ListId} (UserId: {UserId}, Revocation token: {RevocationToken})",
            itemId, listId, userId, revocationToken);

        var giftList = await dbContext.GiftLists
            .Include(l => l.Owner)
            .Include(l => l.Items)
            .ThenInclude(l => l.Claims)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);

        if (giftList is null) return Results.NotFound();

        if (userId is not null && userId == giftList.OwnerId)
        {
            return Results.Forbid();
        }

        var item = giftList.Items
            .FirstOrDefault(i => i.Id == itemId);

        if (item is null) return Results.NotFound();

        if (revocationToken is null && userId is null)
        {
            return Results.BadRequest(new ErrorMessage("Revocation token is required."));
        }

        try
        {
            if (revocationToken is not null)
            {
                item.RemoveClaim(revocationToken.Value, userId);
            }
            else
            {
                item.RemoveClaimByClaimantId(userId!);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorMessage(ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    internal static async Task<IResult> AdoptClaims(AdoptClaimsRequest request,
        ClaimsPrincipal principal, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = principal.GetRequiredUserId();

        if (request.RevocationTokens is not { Count: > 0 })
        {
            return Results.BadRequest(new ErrorMessage("No revocation tokens provided"));
        }

        var distinctTokens = request.RevocationTokens.Distinct().ToList();

        Log.Information("Adopting {Count} unauthenticated claims for user {UserId}", distinctTokens.Count, userId);

        var claims = await dbContext.GiftClaims
            .Include(c => c.Item)
            .ThenInclude(i => i.GiftList)
            .Where(c => c.RevocationToken != null && distinctTokens.Contains(c.RevocationToken.Value))
            .ToListAsync(cancellationToken);

        var filteredClaims = claims
            .Where(c => c.Item.GiftList.OwnerId != userId)
            .Where(c => c.ClaimantId is null || c.ClaimantId == userId)
            .ToList();

        var adoptedClaims = new List<Guid>();
        foreach (var claim in filteredClaims)
        {
            var revocationToken = claim.RevocationToken!.Value;
            claim.AssignToUser(userId);
            adoptedClaims.Add(revocationToken);
        }

        if (adoptedClaims.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        Log.Information("Successfully adopted {AdoptedCount} claims for user {UserId}", adoptedClaims.Count, userId);

        return Results.Ok(new AdoptClaimsResponse(adoptedClaims));
    }
}
