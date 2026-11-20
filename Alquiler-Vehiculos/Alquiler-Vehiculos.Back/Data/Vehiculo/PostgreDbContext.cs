using Alquiler_Vehiculos.Generators;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class PostgreDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options) {
    
    public DbSet<VehiculoModel> Vehiculos =>  Set<VehiculoModel>();
    public DbSet<Foto> Fotos => Set<Foto>();
    public DbSet<ConcesionarioModel> Concesionarios => Set<ConcesionarioModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VehiculoModel>(entity => {
            entity.Property(v => v.Id)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();

            entity.HasOne<Foto>()
                .WithOne()
                .HasForeignKey<VehiculoModel>(v => v.FotoFk)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(v => v.Matrícula)
                .HasMaxLength(7)
                .IsRequired();
            
            entity.Property(v => v.Marca).IsRequired();
            entity.Property(v => v.Modelo).IsRequired();
            entity.Property(v => v.Acriss).IsRequired();
            entity.Property(v => v.Carroceria).IsRequired();
            entity.Property(v => v.Combustible).IsRequired();
            entity.Property(v => v.Transmision).IsRequired();
            entity.Property(v => v.Estado).IsRequired();
            entity.Property(v => v.Color).IsRequired();
            
            entity.Property(v => v.PrecioBaseDia)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(v => v.Plazas).IsRequired();
            entity.Property(v => v.MaleteroLitros).IsRequired();
            entity.Property(v => v.AutonomiaKm).IsRequired();
            entity.Property(v => v.Kilometraje).IsRequired();
            entity.Property(v => v.FechaUltimaRevision).IsRequired();

            entity.HasOne<ConcesionarioModel>()
                .WithMany()
                .HasForeignKey(v => v.ConesionarioFk)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(v => v.Activo).IsRequired();
            entity.HasIndex(v => v.Marca);
            entity.HasIndex(v => v.Modelo);
            entity.HasIndex(v => v.Carroceria);
            entity.HasIndex(v => v.Estado);
            entity.HasIndex(v => v.Combustible);
        });
        
        modelBuilder.Entity<Foto>(entity => {
            entity.HasKey(f => f.Ruta);
            entity.Property(f => f.Nombre).IsRequired();
            entity.Property(f => f.Extension).IsRequired();
            entity.Property(f => f.Tamano).IsRequired();
            entity.Property(f => f.Activo).IsRequired();

        });
        
        modelBuilder.Entity<ConcesionarioModel>(entity => {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Nombre).IsRequired();
            entity.Property(c => c.Ciudad).IsRequired();
            entity.Property(c => c.Telefono).IsRequired();
            entity.Property(c => c.Email).IsRequired();
        });
    }
}