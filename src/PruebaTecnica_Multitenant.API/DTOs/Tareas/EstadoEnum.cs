namespace PruebaTecnica_Multitenant.API.DTOs.Tareas;

/// <summary>Estado de una tarea.</summary>
public enum EstadoEnum
{
    /// <summary>La tarea acaba de ser creada y aún no se ha iniciado.</summary>
    Pendiente  = 1,

    /// <summary>La tarea está siendo trabajada actualmente.</summary>
    EnProgreso = 2,

    /// <summary>La tarea fue finalizada. No puede volver a otro estado.</summary>
    Completado = 3
}
