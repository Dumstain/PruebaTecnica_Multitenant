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
public class UsuariosController(AppDbContext db) : ControllerBase
{
    // GET /api/usuarios — devuelve los usuarios de la organización del Admin
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orgId = User.GetOrgId();

        var usuarios = await db.OrganizacionesUsuarios
            .Where(ou => ou.OrganizacionId == orgId)
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
