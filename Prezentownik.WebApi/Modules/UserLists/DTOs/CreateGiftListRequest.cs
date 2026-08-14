namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record CreateGiftListRequest(
    string Name,
    string? Description);
