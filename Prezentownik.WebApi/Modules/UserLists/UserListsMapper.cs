using Prezentownik.WebApi.Models;
using Prezentownik.WebApi.Models.Enums;
using Dto = Prezentownik.WebApi.Modules.UserLists.DTOs;

namespace Prezentownik.WebApi.Modules.UserLists;

public static class UserListsMapper
{
    public static Dto.ListSummaryDto MapToListSummaryDto(GiftList giftList)
        => new(
            giftList.Id,
            giftList.Name,
            giftList.Description);

    public static Dto.ListDetailsDto MapToListDetailsDto(GiftList giftList)
        => new(
            giftList.Id,
            giftList.Name,
            giftList.Description,
            [.. giftList.Items.Select(MapToItemDto)]);

    public static Dto.ItemDto MapToItemDto(Item item)
        => new(
            item.Id,
            item.Name,
            item.Description,
            MapItemType(item.Type),
            item.TargetQuantity,
            item.OrderNumber);

    public static Dto.ItemType MapItemType(ItemType type)
        => type switch
        {
            ItemType.Singular => Dto.ItemType.Singular,
            ItemType.Limited => Dto.ItemType.Limited,
            ItemType.Limitless => Dto.ItemType.Limitless,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static ItemType MapItemTypeDomain(Dto.ItemType type)
        => type switch
        {
            Dto.ItemType.Singular => ItemType.Singular,
            Dto.ItemType.Limited => ItemType.Limited,
            Dto.ItemType.Limitless => ItemType.Limitless,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static Item MapOntoItem(Item item, Dto.UpsertItemRequest request)
    {
        item.UpdateFromRequest(
            request.Name,
            request.Description,
            request.OrderNumber,
            MapItemTypeDomain(request.Type),
            request.TargetQuantity);

        return item;
    }
}
