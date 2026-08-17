namespace Prezentownik.WebApi.Modules.Public.DTOs;

public record PublicClaimDto(
    string? ClaimerName,
    int QuantityClaimed);
