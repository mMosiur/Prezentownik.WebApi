namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpdateGiftListRequest(
    string Name,
    string? Description);
