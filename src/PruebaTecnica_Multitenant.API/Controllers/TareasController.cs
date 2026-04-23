using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.DTOs.Tareas;
using PruebaTecnica_Multitenant.API.Extensions;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Controllers;

[ApiController]
[Route("api/tareas")]
[Authorize]
public class TareasController(AppDbContext db) : ControllerBase
{
    private const int EstadoCompletada = 3;

    // GET /api/tareas?estadoId=1&usuarioId=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? estadoId, [FromQuery] Guid? usuarioId)
    {
        var orgId = User.GetOrgId();

        var query = db.Tareas
            .Where(t => t.OrganizacionId == orgId)
            .Include(t => t.Estado)
            .Include(t => t.Prioridad)
            .Include(t => t.Usuario)
            .AsQueryable();

        if (estadoId.HasValue)
            query = query.Where(t => t.EstadoId == estadoId.Value);

        if (usuarioId.HasValue)
            query = query.Where(t => t.UsuarioId == usuarioId.Value);

        var tareas = await query.Select(t => MapToResponse(t)).ToListAsync();
        return Ok(tareas);
    }

    // GET /api/tareas/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tarea = await FindInOrg(id);
        return tarea is null ? NotFound() : Ok(MapToResponse(tarea));
    }

    // POST /api/tareas
    [HttpPost]
    public async Task<IActionResult> Create(CreateTareaRequest request)
    {
        var orgId  = User.GetOrgId();
        var userId = User.GetUserId();

        // Miembro solo puede crear tareas asignadas a sí mismo
        if (!User.IsAdmin() && request.UsuarioId != userId)
            return Forbid();

        // El usuario asignado debe pertenecer a la organización
        var asignadoEnOrg = await db.OrganizacionesUsuarios
            .AnyAsync(ou => ou.OrganizacionId == orgId && ou.UsuarioId == request.UsuarioId);

        if (!asignadoEnOrg)
            return BadRequest(new { message = "El usuario asignado no pertenece a la organización." });

        var tarea = new Tarea
        {
            Id              = Guid.NewGuid(),
            OrganizacionId  = orgId,
            UsuarioId       = request.UsuarioId,
            Titulo          = request.Titulo,
            Descripcion     = request.Descripcion,
            EstadoId        = 1, // Siempre inicia en Pendiente
            PrioridadId     = request.PrioridadId,
            FechaDeCreacion = DateTime.UtcNow,
            FechaLimite     = request.FechaLimite
        };

        db.Tareas.Add(tarea);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tarea.Id }, new { id = tarea.Id });
    }

    // PUT /api/tareas/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTareaRequest request)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null) return NotFound();

        // Miembro solo puede editar sus propias tareas
        if (!User.IsAdmin() && tarea.UsuarioId != User.GetUserId())
            return Forbid();

        tarea.Titulo       = request.Titulo;
        tarea.Descripcion  = request.Descripcion;
        tarea.PrioridadId  = request.PrioridadId;
        tarea.FechaLimite  = request.FechaLimite;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/tareas/{id} — solo Admin
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null) return NotFound();

        db.Tareas.Remove(tarea);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/tareas/{id}/estado
    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoRequest request)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null) return NotFound();

        // Una tarea Completada no puede volver a otro estado
        if (tarea.EstadoId == EstadoCompletada)
            return BadRequest(new { message = "Una tarea Completada no puede cambiar de estado." });

        // Solo el Admin o el usuario asignado pueden cambiar el estado
        if (!User.IsAdmin() && tarea.UsuarioId != User.GetUserId())
            return Forbid();

        tarea.EstadoId = request.EstadoId;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Tarea?> FindInOrg(Guid id) =>
        await db.Tareas
            .Include(t => t.Estado)
            .Include(t => t.Prioridad)
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizacionId == User.GetOrgId());

    private static TareaResponse MapToResponse(Tarea t) => new()
    {
        Id              = t.Id,
        OrganizacionId  = t.OrganizacionId,
        UsuarioId       = t.UsuarioId,
        UsuarioEmail    = t.Usuario.Email,
        Titulo          = t.Titulo,
        Descripcion     = t.Descripcion,
        EstadoId        = t.EstadoId,
        Estado          = t.Estado.Nombre,
        PrioridadId     = t.PrioridadId,
        Prioridad       = t.Prioridad.Nombre,
        FechaDeCreacion = t.FechaDeCreacion,
        FechaLimite     = t.FechaLimite
    };
}
