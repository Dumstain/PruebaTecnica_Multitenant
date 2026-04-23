using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    private string Secret   => config["Jwt:Secret"]!;
    private string Issuer   => config["Jwt:Issuer"]!;
    private string Audience => config["Jwt:Audience"]!;
    private int    Expiry   => config.GetValue<int>("Jwt:ExpiryMinutes", 60);

    public TokenResult GenerateToken(Usuario usuario, Guid organizacionId, string rol)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("org", organizacionId.ToString()),
            new Claim(ClaimTypes.Role, rol)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(Expiry);
        return new TokenResult(BuildToken(claims, expiresAt), expiresAt);
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

        return BuildToken(claims, DateTime.UtcNow.AddMinutes(5));
    }

    public ClaimsPrincipal? ValidateSelectionToken(string token)
    {
        var handler    = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Issuer,
            ValidAudience            = Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret))
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);

            // Debe tener "orgs" y NO "org" para evitar que se pase un JWT completo
            if (principal.FindFirst("orgs") is null)  return null;
            if (principal.FindFirst("org")  is not null) return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
