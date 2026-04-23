using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Services;

public interface ITokenService
{
    string GenerateToken(Usuario usuario, Guid organizacionId, string rol);
}
