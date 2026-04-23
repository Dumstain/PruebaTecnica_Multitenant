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

        var membresia = new OrganizacionUsuario
        {
            OrganizacionId = organizacion.Id,
            UsuarioId      = usuario.Id,
            RolId          = 1 // Admin
        };

        db.Usuarios.Add(usuario);
        db.Organizaciones.Add(organizacion);
        db.OrganizacionesUsuarios.Add(membresia);
        await db.SaveChangesAsync();

        var expiry = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");
        var token  = tokenService.GenerateToken(usuario, organizacion.Id, "Admin");

        return Ok(new AuthResponse { Token = token, ExpiresAt = DateTime.UtcNow.AddMinutes(expiry) });
    }

    // Login con contexto de organización — el rol queda embebido en el JWT
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password))
            return Unauthorized(new { message = "Credenciales inválidas." });

        var membresia = await db.OrganizacionesUsuarios
            .Include(x => x.Rol)
            .FirstOrDefaultAsync(x => x.UsuarioId      == usuario.Id
                                   && x.OrganizacionId == request.OrganizacionId);

        if (membresia is null)
            return Unauthorized(new { message = "No perteneces a esta organización." });

        var expiry = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");
        var token  = tokenService.GenerateToken(usuario, request.OrganizacionId, membresia.Rol.Nombre);

        return Ok(new AuthResponse { Token = token, ExpiresAt = DateTime.UtcNow.AddMinutes(expiry) });
    }
}
