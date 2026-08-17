namespace Prezentownik.WebApi.Modules.Public.DTOs;

public record CreateClaimRequest(
    int QuantityClaimed,
    string? ClaimerName);
