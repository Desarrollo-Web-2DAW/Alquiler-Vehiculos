using Alquiler_Vehiculos.Models.Oficina;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Models.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Entities;

/// <summary>
/// Contexto único de la aplicación: Identity y dominio comparten base de datos.
/// Dos DbContext sobre la misma BD romperían la creación del esquema
/// (apuntes 16.6.2), así que todo lo que añada el equipo va aquí.
/// </summary>
public class AppDbContextUsuarios(DbContextOptions<AppDbContextUsuarios> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options) {
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Trabajador> Trabajadores { get; set; } = null!;
    public DbSet<Oficina> Oficinas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // ── Nombres de tabla ─────────────────────────────────────────────────
        // Identity en minúscula (apuntes 16.6.2) y el dominio con el mismo
        // criterio, para que el esquema no mezcle estilos.
        modelBuilder.Entity<User>().ToTable("usuarios");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<Cliente>().ToTable("clientes");
        modelBuilder.Entity<Trabajador>().ToTable("trabajadores");

        // ── Índices únicos ───────────────────────────────────────────────────
        // La última garantía la da la base de datos: la comprobación en el
        // repositorio tiene condición de carrera y no impide duplicados.
        // En PostgreSQL un índice único admite varios NULL, así que los
        // documentos opcionales del cliente no dan problema.
        modelBuilder.Entity<Cliente>(entity => {
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Dni).IsUnique();
            entity.HasIndex(c => c.Nie).IsUnique();
            entity.HasIndex(c => c.Pasaporte).IsUnique();
        });

        modelBuilder.Entity<Trabajador>(entity => {
            entity.HasIndex(t => t.Email).IsUnique();
            entity.HasIndex(t => t.Dni).IsUnique();
        });

        // Oficina única (RF-11): restricción CHECK sobre la clave fija + FK restrictiva al responsable (CU-18).
        modelBuilder.Entity<Oficina>(entity => {
            entity.ToTable("oficina", t => t.HasCheckConstraint(
                "CK_oficina_unica",
                $"\"Id\" = '{Oficina.OficinaUnicaId}'"));

            entity.HasOne(o => o.Responsable)
                .WithMany()
                .HasForeignKey(o => o.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
