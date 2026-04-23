using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Data;
using PruebaTecnica_Multitenant.API.DTOs.Common;
using PruebaTecnica_Multitenant.API.DTOs.Tareas;
using PruebaTecnica_Multitenant.API.Extensions;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Controllers;

[ApiController]
[Route("api/tareas")]
[Authorize]
[Produces("application/json")]
public class TareasController(AppDbContext db, ILogger<TareasController> logger) : ControllerBase
{
    private const int EstadoCompletada = 3;

    /// <summary>
    /// Devuelve todas las tareas de la organización del usuario autenticado.
    /// Ambos roles pueden filtrar por estado y por usuario asignado.
    /// </summary>
    /// <param name="estadoId">Filtro opcional: 1 = Pendiente · 2 = En Progreso · 3 = Completado.</param>
    /// <param name="usuarioId">Filtro opcional por usuario asignado (UUID del miembro).</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<TareaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] EstadoEnum? estadoId, [FromQuery] Guid? usuarioId)
    {
        var query = db.Tareas
            .Where(t => t.OrganizacionId == User.GetOrgId())
            .Include(t => t.Estado)
            .Include(t => t.Prioridad)
            .Include(t => t.Usuario)
            .AsQueryable();

        if (estadoId.HasValue)
            query = query.Where(t => t.EstadoId == (int)estadoId.Value);

        if (usuarioId.HasValue)
            query = query.Where(t => t.UsuarioId == usuarioId.Value);

        return Ok(await query.Select(t => MapToResponse(t)).ToListAsync());
    }

    /// <summary>Obtiene una tarea por ID dentro de la organización del usuario autenticado.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TareaResponse),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null)
            return NotFound(new MessageResponse { Message = "Tarea no encontrada." });

        return Ok(MapToResponse(tarea));
    }

    /// <summary>
    /// Crea una nueva tarea en estado Pendiente.
    /// El campo <c>usuarioId</c> es opcional: si no se envía, la tarea se asigna automáticamente al usuario autenticado.
    /// Admin puede asignarla a cualquier miembro de la org; Miembro solo puede asignársela a sí mismo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object),          StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(CreateTareaRequest request)
    {
        var orgId  = User.GetOrgId();
        var userId = User.GetUserId();

        // Si no se manda usuarioId se asigna al usuario autenticado
        var asignadoId = request.UsuarioId ?? userId;

        // Miembro solo puede asignarse a sí mismo
        if (!User.IsAdmin() && asignadoId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new MessageResponse { Message = "Un Miembro solo puede crear tareas asignadas a sí mismo." });

        if (request.FechaLimite <= DateTime.UtcNow)
            return BadRequest(new MessageResponse { Message = "La fecha límite debe ser una fecha futura." });

        var asignadoEnOrg = await db.OrganizacionesUsuarios
            .AnyAsync(ou => ou.OrganizacionId == orgId && ou.UsuarioId == asignadoId);

        if (!asignadoEnOrg)
            return BadRequest(new MessageResponse { Message = "El usuario asignado no pertenece a la organización." });

        var tarea = new Tarea
        {
            Id              = Guid.NewGuid(),
            OrganizacionId  = orgId,
            UsuarioId       = asignadoId,
            Titulo          = request.Titulo,
            Descripcion     = request.Descripcion,
            EstadoId        = 1, // Siempre inicia en Pendiente
            PrioridadId     = request.PrioridadId,
            FechaDeCreacion = DateTime.UtcNow,
            FechaLimite     = request.FechaLimite
        };

        db.Tareas.Add(tarea);
        await db.SaveChangesAsync();

        logger.LogInformation("Tarea {TareaId} creada por {UserId} en org {OrgId}", tarea.Id, userId, orgId);
        return CreatedAtAction(nameof(GetById), new { id = tarea.Id }, new { id = tarea.Id });
    }

    /// <summary>
    /// Actualiza título, descripción, prioridad y fecha límite de una tarea.
    /// Admin puede editar cualquier tarea; Miembro solo las suyas.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, UpdateTareaRequest request)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null)
            return NotFound(new MessageResponse { Message = "Tarea no encontrada." });

        if (!User.IsAdmin() && tarea.UsuarioId != User.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new MessageResponse { Message = "Solo puedes editar tus propias tareas." });

        if (request.FechaLimite <= DateTime.UtcNow)
            return BadRequest(new MessageResponse { Message = "La fecha límite debe ser una fecha futura." });

        tarea.Titulo      = request.Titulo;
        tarea.Descripcion = request.Descripcion;
        tarea.PrioridadId = request.PrioridadId;
        tarea.FechaLimite = request.FechaLimite;

        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Elimina una tarea. Solo Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null)
            return NotFound(new MessageResponse { Message = "Tarea no encontrada." });

        db.Tareas.Remove(tarea);
        await db.SaveChangesAsync();
        logger.LogInformation("Tarea {TareaId} eliminada por admin {UserId}", id, User.GetUserId());
        return NoContent();
    }

    /// <summary>
    /// Cambia el estado de una tarea (Pendiente → En Progreso → Completada).
    /// Solo Admin o el usuario asignado. Una tarea Completada no puede cambiar de estado.
    /// </summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoRequest request)
    {
        var tarea = await FindInOrg(id);
        if (tarea is null)
            return NotFound(new MessageResponse { Message = "Tarea no encontrada." });

        if (tarea.EstadoId == EstadoCompletada)
        {
            logger.LogWarning("Intento de cambiar estado de tarea completada {TareaId} por {UserId}", id, User.GetUserId());
            return BadRequest(new MessageResponse { Message = "Una tarea Completada no puede cambiar de estado." });
        }

        if (!User.IsAdmin() && tarea.UsuarioId != User.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden,
                new MessageResponse { Message = "Solo el Admin o el asignado pueden cambiar el estado." });

        var estadoAnterior = tarea.EstadoId;
        tarea.EstadoId = (int)request.EstadoId;
        await db.SaveChangesAsync();
        logger.LogInformation("Tarea {TareaId}: estado {De} → {A} por {UserId}", id, estadoAnterior, tarea.EstadoId, User.GetUserId());
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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
