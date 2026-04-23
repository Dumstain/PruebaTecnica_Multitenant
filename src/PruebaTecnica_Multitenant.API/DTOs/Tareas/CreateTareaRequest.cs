using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class CreateTareaRequest
{
    [Required]
    public string Titulo { get; set; } = null!;

    [Required]
    public string Descripcion { get; set; } = null!;

    [Required]
    public Guid UsuarioId { get; set; }

    [Required, Range(1, 3)]
    public int PrioridadId { get; set; }

    [Required]
    public DateTime FechaLimite { get; set; }
}
