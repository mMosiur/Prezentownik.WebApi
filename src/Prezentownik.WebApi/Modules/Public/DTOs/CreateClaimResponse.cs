namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Response returned when a gift item is claimed.
/// </summary>
public sealed record CreateClaimResponse(
    Guid? RevocationToken);
