using Alquiler_Vehiculos.Generators;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Data.Vehiculo;

/// <summary>
/// Contexto de PostgreSQL del catálogo: vehículos, fotografías y concesionarios.
/// Utiliza una base de datos distinta de la del contexto de usuarios.
/// </summary>
/// <param name="options">Opciones de conexión y proveedor de Entity Framework Core.</param>
public class PostgreDbContext (DbContextOptions<PostgreDbContext> options) : DbContext(options) {
    
    /// <summary>Vehículos del catálogo.</summary>
    public DbSet<VehiculoModel> Vehiculos => Set<VehiculoModel>();
    /// <summary>Fotografías identificadas por su ruta.</summary>
    public DbSet<Foto> Fotos => Set<Foto>();
    /// <summary>Concesionarios a los que pertenecen los vehículos.</summary>
    public DbSet<ConcesionarioModel> Concesionarios => Set<ConcesionarioModel>();

    /// <summary>
    /// Configura la generación de identificadores, los campos obligatorios, la precisión
    /// de los precios, los índices y las relaciones con borrado restringido.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de persistencia.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VehiculoModel>(entity => {
            entity.Property(v => v.Id)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();

            entity.HasOne(v => v.Foto)
                .WithOne()
                .HasForeignKey<VehiculoModel>(v => v.FotoFk)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(v => v.Matrícula)
                .HasMaxLength(7)
                .IsRequired();
            
            entity.HasIndex(v => v.Matrícula).IsUnique();
            
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
            entity.HasIndex(v => v.Telefono).IsUnique();
            entity.Property(c => c.Email).IsRequired();
        });
    }
}
