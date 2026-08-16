using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Models.Enums;
using Dto = Prezentownik.WebApi.Modules.Public.DTOs;

namespace Prezentownik.WebApi.Modules.Public;

internal static class PublicMapper
{
    public static Dto.PublicListDto MapToPublicListDto(GiftList giftList)
        => new(
            giftList.Name,
            giftList.Description,
            [.. giftList.Items.Select(MapToPublicItemDto)]);


    public static Dto.PublicItemDto MapToPublicItemDto(Item item)
        => new(item.Id,
            item.Name,
            item.Description,
            MapItemType(item.Type),
            item.TargetQuantity,
            item.OrderNumber,
            item.TotalClaimsQuantity,
            [.. item.Claims.Select(MapToPublicClaimDto)]);

    private static Dto.ItemType MapItemType(ItemType itemType)
        => itemType switch
        {
            ItemType.Singular => Dto.ItemType.Singular,
            ItemType.Limited => Dto.ItemType.Limited,
            ItemType.Limitless => Dto.ItemType.Limitless,
            _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
        };

    private static Dto.PublicClaimDto MapToPublicClaimDto(GiftClaim giftClaim)
        => new(
            giftClaim.ClaimerName,
            giftClaim.QuantityClaimed);
}
