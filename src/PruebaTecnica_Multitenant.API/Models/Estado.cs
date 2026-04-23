namespace PruebaTecnica_Multitenant.API.Models;

public class Estado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<Tarea> Tareas { get; set; } = [];
}
