namespace Prezentownik.WebApi.Modules.Auth.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName);
