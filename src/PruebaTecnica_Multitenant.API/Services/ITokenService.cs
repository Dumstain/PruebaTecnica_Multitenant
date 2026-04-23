using System.Security.Claims;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Services;

public record TokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    TokenResult GenerateToken(Usuario usuario, Guid organizacionId, string rol);
    string GenerateSelectionToken(Usuario usuario, IEnumerable<Guid> orgIds);
    ClaimsPrincipal? ValidateSelectionToken(string token);
}
