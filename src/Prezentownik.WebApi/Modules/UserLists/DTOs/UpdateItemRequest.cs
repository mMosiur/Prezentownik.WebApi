namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpdateItemRequest(
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity);
