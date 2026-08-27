using System.Security.Claims;

namespace Prezentownik.WebApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public string? GetUserId()
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public string GetRequiredUserId()
        {
            return principal.GetUserId()
                ?? throw new InvalidOperationException("User ID was not found for endpoint requiring authentication");
        }
    }
}
