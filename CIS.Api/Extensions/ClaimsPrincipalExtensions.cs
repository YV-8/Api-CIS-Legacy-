using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CIS.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN",
        "OWNER"
    };

    public static string? GetSubjectId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }

    public static string? GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("role")?.Value;
    }

    public static bool IsPrivilegedRole(this ClaimsPrincipal principal)
    {
        var role = principal.GetRole();
        return role is not null && PrivilegedRoles.Contains(role);
    }
}
