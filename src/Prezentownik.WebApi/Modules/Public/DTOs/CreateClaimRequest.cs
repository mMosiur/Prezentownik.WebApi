using System.ComponentModel.DataAnnotations;

namespace Prezentownik.WebApi.Modules.Public.DTOs;

/// <summary>
/// Payload for claiming a gift item.
/// </summary>
public sealed record CreateClaimRequest(
    [Range(1, int.MaxValue)] int QuantityClaimed,
    [StringLength(64)] string? ClaimantName);
