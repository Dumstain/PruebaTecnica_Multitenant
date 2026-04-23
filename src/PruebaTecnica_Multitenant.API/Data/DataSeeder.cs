using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Data;

public static class DataSeeder
{
    // Contraseña de todos los usuarios de prueba
    private const string DefaultPassword = "Password123!";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Organizaciones.AnyAsync()) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        var now  = DateTime.UtcNow;

        // ── Usuarios ─────────────────────────────────────────────────────────
        // Org 1 — Alpha Corp
        var alice = U("alice@example.com", hash);
        var bob   = U("bob@example.com",   hash);
        var carol = U("carol@example.com", hash);
        var david = U("david@example.com", hash);

        // Org 2 — Beta Solutions
        var frank = U("frank@example.com", hash);
        var grace = U("grace@example.com", hash);
        var henry = U("henry@example.com", hash);

        // Org 3 — Gamma Labs
        var ivan  = U("ivan@example.com",  hash);
        var julia = U("julia@example.com", hash);
        var karen = U("karen@example.com", hash);

        // ── CASO ESPECIAL ────────────────────────────────────────────────────
        // eve@example.com es Admin en Org 2 (Beta Solutions)
        //                    y Miembro en Org 3 (Gamma Labs)
        var eve = U("eve@example.com", hash);

        db.Usuarios.AddRange(alice, bob, carol, david, eve, frank, grace, henry, ivan, julia, karen);

        // ── Organizaciones ───────────────────────────────────────────────────
        var org1 = new Organizacion { Id = Guid.NewGuid(), Nombre = "Alpha Corp" };
        var org2 = new Organizacion { Id = Guid.NewGuid(), Nombre = "Beta Solutions" };
        var org3 = new Organizacion { Id = Guid.NewGuid(), Nombre = "Gamma Labs" };

        db.Organizaciones.AddRange(org1, org2, org3);

        // ── Membresías ───────────────────────────────────────────────────────
        db.OrganizacionesUsuarios.AddRange(
            // Org 1: 2 Admin, 2 Miembro — todos exclusivos de esta org
            M(org1, alice, rolId: 1),
            M(org1, bob,   rolId: 1),
            M(org1, carol, rolId: 2),
            M(org1, david, rolId: 2),

            // Org 2: 2 Admin, 2 Miembro — todos exclusivos de esta org (excepto eve)
            M(org2, eve,   rolId: 1),  // Admin aquí
            M(org2, frank, rolId: 1),
            M(org2, grace, rolId: 2),
            M(org2, henry, rolId: 2),

            // Org 3: 2 Admin, 2 Miembro propios + eve como Miembro (caso especial)
            M(org3, ivan,  rolId: 1),
            M(org3, julia, rolId: 1),
            M(org3, karen, rolId: 2),
            M(org3, eve,   rolId: 2)   // Miembro aquí aunque es Admin en Org 2
        );

        // ── Tareas ───────────────────────────────────────────────────────────
        // Org 1 — estados variados, asignadas a miembros
        db.Tareas.AddRange(
            T(org1, carol, "Diseñar logo corporativo",
              "Crear propuestas de identidad visual para Alpha Corp",
              estadoId: 1, prioridadId: 3, now, now.AddDays(7)),

            T(org1, david, "Revisar contratos Q2",
              "Auditar contratos del segundo trimestre con área legal",
              estadoId: 2, prioridadId: 2, now, now.AddDays(3)),

            T(org1, carol, "Entregar informe Q1",
              "Informe consolidado de resultados del primer trimestre",
              estadoId: 3, prioridadId: 1, now.AddDays(-10), now.AddDays(-1))
        );

        // Org 2 — estados variados, asignadas a miembros
        db.Tareas.AddRange(
            T(org2, grace, "Configurar servidor de producción",
              "Setup de instancia EC2 en AWS para el ambiente productivo",
              estadoId: 1, prioridadId: 3, now, now.AddDays(5)),

            T(org2, henry, "Actualizar documentación API",
              "Documentar los nuevos endpoints en el portal de desarrolladores",
              estadoId: 2, prioridadId: 2, now, now.AddDays(2)),

            T(org2, grace, "Deploy versión 2.0",
              "Release de la versión 2.0 en producción con zero-downtime",
              estadoId: 3, prioridadId: 3, now.AddDays(-5), now.AddDays(-2))
        );

        // Org 3 — una tarea asignada a eve (demuestra su membresía como Miembro)
        db.Tareas.AddRange(
            T(org3, karen, "Análisis de segmentación de clientes",
              "Segmentar base de clientes por comportamiento de compra",
              estadoId: 1, prioridadId: 2, now, now.AddDays(10)),

            T(org3, eve, "Reunión con cliente estratégico",
              "Preparar presentación para el cliente principal de Gamma Labs",
              estadoId: 2, prioridadId: 3, now, now.AddDays(1)),

            T(org3, karen, "Reporte mensual de métricas",
              "Consolidar KPIs del mes en el dashboard ejecutivo",
              estadoId: 3, prioridadId: 1, now.AddDays(-7), now.AddDays(-3))
        );

        await db.SaveChangesAsync();

        Console.WriteLine("✔ Seed completado. Credenciales de prueba:");
        Console.WriteLine($"  Contraseña para todos: {DefaultPassword}");
        Console.WriteLine("  Org 1 — Alpha Corp     : alice@, bob@ (Admin) | carol@, david@ (Miembro)");
        Console.WriteLine("  Org 2 — Beta Solutions : eve@, frank@ (Admin) | grace@, henry@ (Miembro)");
        Console.WriteLine("  Org 3 — Gamma Labs     : ivan@, julia@ (Admin) | karen@ (Miembro) | eve@ (Miembro — Admin en Org 2)");
        Console.WriteLine("  Dominio de todos: @example.com");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Usuario U(string email, string hash) =>
        new() { Id = Guid.NewGuid(), Email = email, Password = hash };

    private static OrganizacionUsuario M(Organizacion org, Usuario user, int rolId) =>
        new() { OrganizacionId = org.Id, UsuarioId = user.Id, RolId = rolId };

    private static Tarea T(
        Organizacion org, Usuario user,
        string titulo, string descripcion,
        int estadoId, int prioridadId,
        DateTime creacion, DateTime limite) => new()
    {
        Id              = Guid.NewGuid(),
        OrganizacionId  = org.Id,
        UsuarioId       = user.Id,
        Titulo          = titulo,
        Descripcion     = descripcion,
        EstadoId        = estadoId,
        PrioridadId     = prioridadId,
        FechaDeCreacion = creacion,
        FechaLimite     = limite
    };
}
