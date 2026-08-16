namespace Prezentownik.WebApi.Modules.Auth.DTOs;

public record UserInfoResponse(
    string Email,
    string? DisplayName);
