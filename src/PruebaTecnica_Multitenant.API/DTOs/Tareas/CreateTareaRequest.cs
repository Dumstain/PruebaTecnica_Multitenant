using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class CreateTareaRequest
{
    [Required]
    public string Titulo { get; set; } = null!;

    [Required]
    public string Descripcion { get; set; } = null!;

    /// <summary>
    /// (Opcional) ID del usuario al que se asigna la tarea.
    /// Si no se envía, la tarea queda asignada al usuario autenticado.
    /// Un Miembro solo puede omitirlo o enviar su propio ID; solo un Admin puede asignar a otro usuario.
    /// </summary>
    public Guid? UsuarioId { get; set; }

    [Required, Range(1, 3)]
    public int PrioridadId { get; set; }

    [Required]
    public DateTime FechaLimite { get; set; }
}
