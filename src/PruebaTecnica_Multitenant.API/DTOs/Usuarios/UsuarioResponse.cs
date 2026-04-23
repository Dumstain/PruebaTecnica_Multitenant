namespace PruebaTecnica_Multitenant.API.DTOs.Usuarios;

public class UsuarioResponse
{
    public Guid   Id    { get; set; }
    public string Email { get; set; } = null!;
    public string Rol   { get; set; } = null!;
}
