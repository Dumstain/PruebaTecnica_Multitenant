namespace PruebaTecnica_Multitenant.API.Models;

public class OrganizacionUsuario
{
    public Guid OrganizacionId { get; set; }
    public Guid UsuarioId { get; set; }
    public int RolId { get; set; }

    public Organizacion Organizacion { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}
