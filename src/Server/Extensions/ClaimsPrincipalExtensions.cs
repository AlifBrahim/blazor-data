using System.Security.Claims;

namespace Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Authenticated user is missing NameIdentifier claim.");
        }

        return value;
    }
}
