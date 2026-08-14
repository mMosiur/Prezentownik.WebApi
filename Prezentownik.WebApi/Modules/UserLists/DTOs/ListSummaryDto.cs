namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record ListSummaryDto(
    Guid Id,
    string Name,
    string? Description);
