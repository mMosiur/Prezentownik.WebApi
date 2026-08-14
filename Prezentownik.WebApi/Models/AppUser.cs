using Microsoft.AspNetCore.Identity;

namespace Prezentownik.WebApi.Models;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
