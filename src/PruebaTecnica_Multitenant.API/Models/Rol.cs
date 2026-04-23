namespace PruebaTecnica_Multitenant.API.Models;

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<OrganizacionUsuario> OrganizacionesUsuarios { get; set; } = [];
}
