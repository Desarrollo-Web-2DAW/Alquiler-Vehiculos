using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Models.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Entities;

/// <summary>
/// Contexto de PostgreSQL para Identity, clientes y trabajadores.
/// El catálogo de vehículos utiliza otro contexto y otra base de datos.
/// </summary>
/// <param name="options">Opciones de conexión y proveedor de Entity Framework Core.</param>
public class AppDbContextUsuarios(DbContextOptions<AppDbContextUsuarios> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options) {
    /// <summary>Clientes asociados a las cuentas de usuario.</summary>
    public DbSet<Cliente> Clientes { get; set; } = null!;
    /// <summary>Trabajadores asociados a las cuentas de usuario.</summary>
    public DbSet<Trabajador> Trabajadores { get; set; } = null!;

    /// <summary>
    /// Configura las tablas de Identity y del dominio de usuarios y sus índices únicos.
    /// Conserva la configuración base de Identity antes de aplicar la propia.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de persistencia.</param>
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
    }
}
