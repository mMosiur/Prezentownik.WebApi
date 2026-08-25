namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Represents a public gift list view.
/// </summary>
public sealed record PublicListDto(
    string Name,
    string? Description,
    string? OwnerDisplayName,
    List<PublicItemDto> Items);
