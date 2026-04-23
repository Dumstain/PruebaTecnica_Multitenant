using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class CambiarEstadoRequest
{
    /// <summary>Nuevo estado de la tarea. Una tarea Completada no puede cambiar de estado.</summary>
    [Required]
    public EstadoEnum EstadoId { get; set; }
}
