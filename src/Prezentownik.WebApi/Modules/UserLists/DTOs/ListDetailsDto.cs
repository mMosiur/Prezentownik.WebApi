namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record ListDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    List<ItemDto> Items);
