namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Represents a claim on a gift item in a public list.
/// </summary>
public sealed record PublicClaimDto(
    string? ClaimerName,
    int QuantityClaimed,
    bool IsMyClaim = false,
    Guid? RevocationToken = null);
