namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpsertItemRequest(
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity);
