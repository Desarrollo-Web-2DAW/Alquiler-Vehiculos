using Alquiler_Vehiculos.Generators;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Data.Vehiculo;

public class PostgreDbContext : IdentityDbContext<AppUser, IdentityRole<long>, long>(options) {
    
    public DbSet<Models.Vehiculo.Vehiculo> Vehiculos =>  Set<Models.Vehiculo.Vehiculo>();
    public DbSet<Foto> Fotos => Set<Foto>();
    public DbSet<Concesionario> Concesionarios => Set<Concesionario>();
    
    public override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Models.Vehiculo.Vehiculo>(entity => {
            entity.Property(p => p.Id)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();
            
            entity.HasOne(p => p.Foto)
                .WithOne()
                .HasForeignKey<Models.Vehiculo.Vehiculo>(p => p.FotoFk)
                .OnDelete(DeleteBehavior.Restrict);
            
            
        })
    }
}