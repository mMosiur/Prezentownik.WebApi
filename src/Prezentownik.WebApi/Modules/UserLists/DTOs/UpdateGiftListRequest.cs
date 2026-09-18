using System.ComponentModel.DataAnnotations;

namespace Prezentownik.WebApi.Modules.UserLists.DTOs;

public record UpdateGiftListRequest(
    [Required, StringLength(128)] string Name,
    [StringLength(1024)] string? Description);
