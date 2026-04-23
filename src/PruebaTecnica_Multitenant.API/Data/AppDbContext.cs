using Microsoft.EntityFrameworkCore;
using PruebaTecnica_Multitenant.API.Models;

namespace PruebaTecnica_Multitenant.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<OrganizacionUsuario> OrganizacionesUsuarios => Set<OrganizacionUsuario>();
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Prioridad> Prioridades => Set<Prioridad>();
    public DbSet<Tarea> Tareas => Set<Tarea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organizacion>(e =>
        {
            e.ToTable("organizacion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.Password).HasColumnName("password").IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("rol");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
        });

        modelBuilder.Entity<OrganizacionUsuario>(e =>
        {
            e.ToTable("organizaciones_usuarios");
            e.HasKey(x => new { x.OrganizacionId, x.UsuarioId });
            e.Property(x => x.OrganizacionId).HasColumnName("organizacion_id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.RolId).HasColumnName("rol_id");

            e.HasOne(x => x.Organizacion)
                .WithMany(x => x.OrganizacionesUsuarios)
                .HasForeignKey(x => x.OrganizacionId);

            e.HasOne(x => x.Usuario)
                .WithMany(x => x.OrganizacionesUsuarios)
                .HasForeignKey(x => x.UsuarioId);

            e.HasOne(x => x.Rol)
                .WithMany(x => x.OrganizacionesUsuarios)
                .HasForeignKey(x => x.RolId);
        });

        modelBuilder.Entity<Estado>(e =>
        {
            e.ToTable("estado");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
        });

        modelBuilder.Entity<Prioridad>(e =>
        {
            e.ToTable("prioridad");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
        });

        modelBuilder.Entity<Tarea>(e =>
        {
            e.ToTable("tarea");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.OrganizacionId).HasColumnName("organizacion_id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x => x.Titulo).HasColumnName("titulo").IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").IsRequired();
            e.Property(x => x.EstadoId).HasColumnName("estado_id");
            e.Property(x => x.PrioridadId).HasColumnName("prioridad_id");
            e.Property(x => x.FechaDeCreacion).HasColumnName("fecha_de_creacion").HasColumnType("timestamp");
            e.Property(x => x.FechaLimite).HasColumnName("fecha_limite").HasColumnType("timestamp");

            e.HasOne(x => x.Organizacion)
                .WithMany(x => x.Tareas)
                .HasForeignKey(x => x.OrganizacionId);

            e.HasOne(x => x.Usuario)
                .WithMany(x => x.Tareas)
                .HasForeignKey(x => x.UsuarioId);

            e.HasOne(x => x.Estado)
                .WithMany(x => x.Tareas)
                .HasForeignKey(x => x.EstadoId);

            e.HasOne(x => x.Prioridad)
                .WithMany(x => x.Tareas)
                .HasForeignKey(x => x.PrioridadId);
        });
    }
}
