namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Payload for adopting unauthenticated gift claims into the authenticated user account.
/// </summary>
public sealed record AdoptClaimsRequest(
    List<Guid> RevocationTokens);
