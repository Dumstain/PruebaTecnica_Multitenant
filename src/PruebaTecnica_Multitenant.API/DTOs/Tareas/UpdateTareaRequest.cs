using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class UpdateTareaRequest
{
    [Required]
    public string Titulo { get; set; } = null!;

    [Required]
    public string Descripcion { get; set; } = null!;

    [Required, Range(1, 3)]
    public int PrioridadId { get; set; }

    [Required]
    public DateTime FechaLimite { get; set; }
}
