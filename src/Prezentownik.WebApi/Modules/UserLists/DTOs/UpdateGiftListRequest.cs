using System.ComponentModel.DataAnnotations;

namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpdateGiftListRequest(
    [Required, MaxLength(128)] string Name,
    [MaxLength(1024)] string? Description);
