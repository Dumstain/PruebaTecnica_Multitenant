using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.DTOs.Auth;
using PruebaTecnica_Multitenant.API.DTOs.Common;
using PruebaTecnica_Multitenant.API.Models;
using PruebaTecnica_Multitenant.API.Services;

namespace PruebaTecnica_Multitenant.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AppDbContext db, ITokenService tokenService, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Registra un nuevo usuario y crea su organización con rol Admin.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse),    StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await db.Usuarios.AnyAsync(u => u.Email == request.Email))
        {
            logger.LogWarning("Registro fallido: el email {Email} ya existe", request.Email);
            return Conflict(new MessageResponse { Message = "El email ya está registrado." });
        }

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

        logger.LogInformation("Usuario {Email} registrado. Org: {OrgId}", request.Email, organizacion.Id);
        var result = tokenService.GenerateToken(usuario, organizacion.Id, "Admin");
        return Ok(new AuthResponse { Token = result.Token, ExpiresAt = result.ExpiresAt });
    }

    /// <summary>
    /// Login por email + password.
    /// Si el usuario pertenece a 1 organización devuelve el JWT directamente.
    /// Si pertenece a 2 o más devuelve un selection token + lista de organizaciones.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse),    StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MultiOrgResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password))
        {
            logger.LogWarning("Login fallido para {Email}", request.Email);
            return Unauthorized(new MessageResponse { Message = "Credenciales inválidas." });
        }

        var membresias = await db.OrganizacionesUsuarios
            .Where(ou => ou.UsuarioId == usuario.Id)
            .Include(ou => ou.Organizacion)
            .Include(ou => ou.Rol)
            .ToListAsync();

        if (membresias.Count == 0)
        {
            logger.LogWarning("Login fallido: {Email} sin organizaciones", usuario.Email);
            return Unauthorized(new MessageResponse { Message = "El usuario no pertenece a ninguna organización." });
        }

        // Una sola organización → JWT directo
        if (membresias.Count == 1)
        {
            var m      = membresias[0];
            var result = tokenService.GenerateToken(usuario, m.OrganizacionId, m.Rol.Nombre);
            logger.LogInformation("Login exitoso: {Email} → org {OrgId} ({Rol})", usuario.Email, m.OrganizacionId, m.Rol.Nombre);
            return Ok(new AuthResponse { Token = result.Token, ExpiresAt = result.ExpiresAt });
        }

        // Varias organizaciones → selection token + lista para elegir
        var selectionToken = tokenService.GenerateSelectionToken(
            usuario, membresias.Select(m => m.OrganizacionId));

        logger.LogInformation("Login multi-org: {Email} tiene {Count} organizaciones, se emitió selection token", usuario.Email, membresias.Count);
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

    /// <summary>Segunda fase del login multi-org: elige organización con el selection token.</summary>
    [HttpPost("login/organizacion")]
    [ProducesResponseType(typeof(AuthResponse),    StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SelectOrganizacion(SelectOrgRequest request)
    {
        var principal = tokenService.ValidateSelectionToken(request.SelectionToken);
        if (principal is null)
        {
            logger.LogWarning("Selection token inválido o expirado en /login/organizacion");
            return Unauthorized(new MessageResponse { Message = "Selection token inválido o expirado." });
        }

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var orgsPermitidas = principal.FindFirst("orgs")!.Value
            .Split(',')
            .Select(Guid.Parse)
            .ToHashSet();

        if (!orgsPermitidas.Contains(request.OrganizacionId))
        {
            logger.LogWarning("Usuario {UserId} intentó seleccionar org {OrgId} que no le pertenece", userId, request.OrganizacionId);
            return Unauthorized(new MessageResponse { Message = "No perteneces a esa organización." });
        }

        var usuario = await db.Usuarios.FindAsync(userId);
        if (usuario is null)
            return Unauthorized(new MessageResponse { Message = "Usuario no encontrado." });

        var membresia = await db.OrganizacionesUsuarios
            .Include(ou => ou.Rol)
            .FirstOrDefaultAsync(ou => ou.UsuarioId      == userId
                                    && ou.OrganizacionId == request.OrganizacionId);

        if (membresia is null)
            return Unauthorized(new MessageResponse { Message = "No perteneces a esa organización." });

        var result = tokenService.GenerateToken(usuario, request.OrganizacionId, membresia.Rol.Nombre);
        logger.LogInformation("Selección de org exitosa: {Email} → org {OrgId} ({Rol})", usuario.Email, request.OrganizacionId, membresia.Rol.Nombre);
        return Ok(new AuthResponse { Token = result.Token, ExpiresAt = result.ExpiresAt });
    }
}
