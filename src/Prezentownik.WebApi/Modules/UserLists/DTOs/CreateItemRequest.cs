namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record CreateItemRequest(
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity);
