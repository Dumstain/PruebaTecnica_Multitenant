using System.Security.Claims;

namespace PruebaTecnica_Multitenant.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static Guid GetOrgId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst("org")!.Value);

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value == "Admin";
}
