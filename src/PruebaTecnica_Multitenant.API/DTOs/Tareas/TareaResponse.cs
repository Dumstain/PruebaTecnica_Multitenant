namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

public class TareaResponse
{
    public Guid     Id              { get; set; }
    public Guid     OrganizacionId  { get; set; }
    public Guid     UsuarioId       { get; set; }
    public string   UsuarioEmail    { get; set; } = null!;
    public string   Titulo          { get; set; } = null!;
    public string   Descripcion     { get; set; } = null!;
    public int      EstadoId        { get; set; }
    public string   Estado          { get; set; } = null!;
    public int      PrioridadId     { get; set; }
    public string   Prioridad       { get; set; } = null!;
    public DateTime FechaDeCreacion { get; set; }
    public DateTime FechaLimite     { get; set; }
}
