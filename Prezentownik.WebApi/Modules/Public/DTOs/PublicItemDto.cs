namespace Prezentownik.WebApi.Modules.Public.DTOs;

public record PublicItemDto(
    Guid Id,
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity,
    int OrderNumber,
    int TotalClaimed,
    List<PublicClaimDto> Claims);
