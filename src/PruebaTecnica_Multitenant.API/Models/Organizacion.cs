namespace PruebaTecnica_Multitenant.API.Models;

public class Organizacion
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<OrganizacionUsuario> OrganizacionesUsuarios { get; set; } = [];
    public ICollection<Tarea> Tareas { get; set; } = [];
}
