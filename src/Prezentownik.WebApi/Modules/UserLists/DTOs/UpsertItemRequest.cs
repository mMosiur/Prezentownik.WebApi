using System.ComponentModel.DataAnnotations;

namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpsertItemRequest(
    [Required, StringLength(128)] string Name,
    [StringLength(1024)] string? Description,
    ItemType Type,
    [Range(1, int.MaxValue)] int? TargetQuantity);
