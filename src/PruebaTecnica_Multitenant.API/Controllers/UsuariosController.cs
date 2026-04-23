using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.DTOs.Usuarios;
using PruebaTecnica_Multitenant.API.Extensions;

namespace PruebaTecnica_Multitenant.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    /// <summary>Lista todos los usuarios de la organización del Admin autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UsuarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await db.OrganizacionesUsuarios
            .Where(ou => ou.OrganizacionId == User.GetOrgId())
            .Include(ou => ou.Usuario)
            .Include(ou => ou.Rol)
            .Select(ou => new UsuarioResponse
            {
                Id    = ou.Usuario.Id,
                Email = ou.Usuario.Email,
                Rol   = ou.Rol.Nombre
            })
            .ToListAsync();

        return Ok(usuarios);
    }
}
