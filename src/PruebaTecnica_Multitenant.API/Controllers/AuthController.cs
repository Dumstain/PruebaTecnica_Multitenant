using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.DTOs.Auth;
using PruebaTecnica_Multitenant.API.Models;
using PruebaTecnica_Multitenant.API.Services;

namespace PruebaTecnica_Multitenant.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokenService) : ControllerBase
{
    // Crea usuario + organización y le asigna rol Admin
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await db.Usuarios.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "El email ya está registrado." });

        var usuario = new Usuario
        {
            Id       = Guid.NewGuid(),
            Email    = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        var organizacion = new Organizacion
        {
            Id     = Guid.NewGuid(),
            Nombre = request.NombreOrganizacion
        };

        db.Usuarios.Add(usuario);
        db.Organizaciones.Add(organizacion);
        db.OrganizacionesUsuarios.Add(new OrganizacionUsuario
        {
            OrganizacionId = organizacion.Id,
            UsuarioId      = usuario.Id,
            RolId          = 1 // Admin
        });
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(usuario, organizacion.Id, "Admin");
        return Ok(new AuthResponse { Token = token, ExpiresAt = ExpiresAt() });
    }

    // Login inteligente:
    //   1 organización  → JWT completo de inmediato
    //   2+ organizaciones → selection token + lista de orgs para elegir
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password))
            return Unauthorized(new { message = "Credenciales inválidas." });

        var membresias = await db.OrganizacionesUsuarios
            .Where(ou => ou.UsuarioId == usuario.Id)
            .Include(ou => ou.Organizacion)
            .Include(ou => ou.Rol)
            .ToListAsync();

        if (membresias.Count == 0)
            return Unauthorized(new { message = "El usuario no pertenece a ninguna organización." });

        // Caso: única organización — login automático
        if (membresias.Count == 1)
        {
            var m     = membresias[0];
            var token = tokenService.GenerateToken(usuario, m.OrganizacionId, m.Rol.Nombre);
            return Ok(new AuthResponse { Token = token, ExpiresAt = ExpiresAt() });
        }

        // Caso: varias organizaciones — devuelve selection token + lista para elegir
        var selectionToken = tokenService.GenerateSelectionToken(
            usuario, membresias.Select(m => m.OrganizacionId));

        return Ok(new MultiOrgResponse
        {
            SelectionToken = selectionToken,
            Organizaciones = membresias.Select(m => new OrganizacionItem
            {
                Id     = m.OrganizacionId,
                Nombre = m.Organizacion.Nombre
            }).ToList()
        });
    }

    // Segunda fase del login multi-org:
    // Recibe el selection token + el ID de la org elegida → devuelve JWT completo
    [HttpPost("login/organizacion")]
    public async Task<IActionResult> SelectOrganizacion(SelectOrgRequest request)
    {
        var principal = tokenService.ValidateSelectionToken(request.SelectionToken);
        if (principal is null)
            return Unauthorized(new { message = "Selection token inválido o expirado." });

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var orgsPermitidas = principal.FindFirst("orgs")!.Value
            .Split(',')
            .Select(Guid.Parse)
            .ToHashSet();

        if (!orgsPermitidas.Contains(request.OrganizacionId))
            return Unauthorized(new { message = "No perteneces a esa organización." });

        var usuario = await db.Usuarios.FindAsync(userId);
        if (usuario is null) return Unauthorized();

        var membresia = await db.OrganizacionesUsuarios
            .Include(ou => ou.Rol)
            .FirstOrDefaultAsync(ou => ou.UsuarioId      == userId
                                    && ou.OrganizacionId == request.OrganizacionId);

        if (membresia is null)
            return Unauthorized(new { message = "No perteneces a esa organización." });

        var token = tokenService.GenerateToken(usuario, request.OrganizacionId, membresia.Rol.Nombre);
        return Ok(new AuthResponse { Token = token, ExpiresAt = ExpiresAt() });
    }

    private static DateTime ExpiresAt() =>
        DateTime.UtcNow.AddMinutes(
            int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60"));
}
