namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Represents a gift item in a public list.
/// </summary>
public sealed record PublicItemDto(
    Guid Id,
    string Name,
    string? Description,
    ItemType Type,
    int? TargetQuantity,
    int OrderNumber,
    int TotalClaimed,
    List<PublicClaimDto> Claims,
    bool IsClaimedByCurrentUser = false);
