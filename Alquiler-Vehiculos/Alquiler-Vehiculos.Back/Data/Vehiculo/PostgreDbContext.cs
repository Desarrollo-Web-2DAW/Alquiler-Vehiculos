using Alquiler_Vehiculos.Generators;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Data.Vehiculo;

public class PostgreDbContext : IdentityDbContext<AppUser, IdentityRole<long>, long>(options) {
    
    public DbSet<Models.Vehiculo.VehiculoModel> Vehiculos =>  Set<Models.Vehiculo.VehiculoModel>();
    public DbSet<MarcaModel> Marcas => Set<MarcaModel>();
    public DbSet<ConcesionarioModel> Concesionarios => Set<ConcesionarioModel>();
    
    public override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Models.Vehiculo.VehiculoModel>(entity => {
            entity.Property(p => p.Id)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();
            entity.HasOne(p => p.FotoFk)
                .WithMany()
                .HasForeignKey(p => p.)
        })
    }
}