using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Models.Enums;
using Dto = Prezentownik.WebApi.Modules.Public.DTOs;

namespace Prezentownik.WebApi.Modules.Public;

internal static class PublicMapper
{
    public static Dto.PublicListDto MapToPublicListDto(GiftList giftList, AppUser? owner, string? currentUserId = null)
        => new(
            giftList.Name,
            giftList.Description,
            owner?.DisplayName,
            [.. giftList.Items.OrderBy(i => i.OrderNumber).Select(i => MapToPublicItemDto(i, currentUserId))]);

    public static Dto.PublicItemDto MapToPublicItemDto(Item item, string? currentUserId = null)
        => new(
            item.Id,
            item.Name,
            item.Description,
            MapItemType(item.Type),
            item.TargetQuantity,
            item.OrderNumber,
            item.TotalClaimsQuantity,
            [.. item.Claims.Select(c => MapToPublicClaimDto(c, currentUserId))],
            IsClaimedByCurrentUser: currentUserId is not null && item.Claims.Any(c => c.ClaimerId == currentUserId));

    private static Dto.ItemType MapItemType(ItemType itemType)
        => itemType switch
        {
            ItemType.Singular => Dto.ItemType.Singular,
            ItemType.Limited => Dto.ItemType.Limited,
            ItemType.Limitless => Dto.ItemType.Limitless,
            _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
        };

    private static Dto.PublicClaimDto MapToPublicClaimDto(GiftClaim giftClaim, string? currentUserId = null)
    {
        var isMyClaim = currentUserId is not null && giftClaim.ClaimerId == currentUserId;
        var claimerName = giftClaim.ClaimerName ?? giftClaim.Claimer?.DisplayName;
        return new(
            claimerName,
            giftClaim.QuantityClaimed,
            IsMyClaim: isMyClaim,
            RevocationToken: isMyClaim ? giftClaim.RevocationToken : null);
    }
}
