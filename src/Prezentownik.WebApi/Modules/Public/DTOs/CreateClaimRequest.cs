using System.ComponentModel.DataAnnotations;

namespace Prezentownik.WebApi.Modules.Public.DTOs;

public record CreateClaimRequest(
    [Range(1, int.MaxValue)] int QuantityClaimed,
    [MaxLength(64)] string? ClaimerName);
