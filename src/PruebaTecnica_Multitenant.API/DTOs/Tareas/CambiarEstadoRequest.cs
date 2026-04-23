using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class CambiarEstadoRequest
{
    [Required, Range(1, 3)]
    public int EstadoId { get; set; }
}
