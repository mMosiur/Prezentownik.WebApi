namespace Prezentownik.WebApi.Modules.Public.DTOs;

public record PublicListDto(
    string Name,
    string? Description,
    List<PublicItemDto> Items);
