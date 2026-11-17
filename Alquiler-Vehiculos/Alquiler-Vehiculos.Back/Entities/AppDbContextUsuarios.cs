using Alquiler_Vehiculos.Models.Cliente;
using Alquiler_Vehiculos.Models.Trabajador;
using Alquiler_Vehiculos.Models.Usuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Entities;

public class AppDbContextUsuarios(DbContextOptions<AppDbContextUsuarios> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options) {
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Trabajador> Trabajadores { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>().ToTable("usuarios");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");  
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}