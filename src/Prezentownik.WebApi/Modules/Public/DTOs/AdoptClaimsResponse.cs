namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Response containing results of claim adoption.
/// </summary>
public sealed record AdoptClaimsResponse(
    int AdoptedClaimsCount);
