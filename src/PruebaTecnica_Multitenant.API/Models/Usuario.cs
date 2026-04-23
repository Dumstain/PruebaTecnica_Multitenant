namespace PruebaTecnica_Multitenant.API.Models;

public class Usuario
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    public ICollection<OrganizacionUsuario> OrganizacionesUsuarios { get; set; } = [];
    public ICollection<Tarea> Tareas { get; set; } = [];
}
