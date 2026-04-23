using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Services;

public class TokenService : ITokenService
{
    public string GenerateToken(Usuario usuario, Guid organizacionId, string rol)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("org", organizacionId.ToString()),
            new Claim(ClaimTypes.Role, rol)
        };

        return BuildToken(claims, GetExpiry());
    }

    // Token de vida corta (5 min) que solo contiene los IDs de orgs permitidas.
    // No incluye "org" ni "role", por lo que no pasa los endpoints protegidos.
    public string GenerateSelectionToken(Usuario usuario, IEnumerable<Guid> orgIds)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim("orgs", string.Join(",", orgIds))
        };

        return BuildToken(claims, expiryMinutes: 5);
    }

    public ClaimsPrincipal? ValidateSelectionToken(string token)
    {
        var secret   = Environment.GetEnvironmentVariable("JWT_SECRET")!;
        var issuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")!;
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;

        var handler    = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);

            // Debe tener "orgs" y NO tener "org" (para que no se pase un JWT completo)
            if (principal.FindFirst("orgs") is null) return null;
            if (principal.FindFirst("org")  is not null) return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildToken(IEnumerable<Claim> claims, int expiryMinutes)
    {
        var secret   = Environment.GetEnvironmentVariable("JWT_SECRET")!;
        var issuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")!;
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static int GetExpiry() =>
        int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");
}
