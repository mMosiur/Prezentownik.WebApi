namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record ItemDto(
    Guid Id,
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity,
    int OrderNumber);
