namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Response containing results of claim adoption.
/// </summary>
public sealed record AdoptClaimsResponse(
    List<Guid> AdoptedClaimsRevocationTokens);
