namespace PruebaTecnica_Multitenant.API.Models;

public class Tarea
{
    public Guid Id { get; set; }
    public Guid OrganizacionId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Titulo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public int EstadoId { get; set; }
    public int PrioridadId { get; set; }
    public DateTime FechaDeCreacion { get; set; }
    public DateTime FechaLimite { get; set; }

    public Organizacion Organizacion { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Estado Estado { get; set; } = null!;
    public Prioridad Prioridad { get; set; } = null!;
}
