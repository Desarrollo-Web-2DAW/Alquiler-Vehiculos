using Alquiler_Vehiculos.Models.Storage;

namespace Alquiler_Vehiculos.Repositories.Storage;

/// <summary>
/// Repositorio de Fotos que implementa las operaciones CRUD
/// utilizando Entity Framework Core, usando el Nombre como clave primaria.
/// </summary>
public class FotoRepository(AppDbContext context, ILogger<FotoRepository> logger)
{
    /// <summary>Crea una nueva foto.</summary>
    public async Task<Foto> Create(Foto item) {
        logger.LogInformation("Creando Foto: {nombre}", item.Nombre);
        await context.Set<Foto>().AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }
    
    /// <summary>Realiza un borrado lógico de la foto por su nombre.</summary>
    public async Task<bool> SoftDelete(string key) {
        logger.LogInformation("Realizando borrado lógico de Foto ID: {Id}", key);
        return await context.Set<Foto>()
            .Where(f => f.Nombre == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.DeletedAt, DateTime.UtcNow)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)
                .SetProperty(f => f.Activo, false)) > 0;
    }

    /// <summary>Obtiene una foto por su nombre.</summary>
    public async Task<Foto?> GetById(string key) {
        logger.LogDebug("Obteniendo Foto por ID: {Key}", key);
        return await context.Set<Foto>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Nombre == key);
    }

    /// <summary>Actualiza una foto existente.</summary>
    public async Task<Foto?> Update(string key, Foto item) 
    {
        logger.LogInformation("Actualizando Foto ID: {Id}", key);
    
        Foto existente = await context.Set<Foto>().FirstOrDefaultAsync(f => f.Nombre == key);
        if (existente == null || existente.DeletedAt != null) return null;
        
        var actualizado = existente with {
            Ruta = item.Ruta,
            Extension = item.Extension,
            Tamano = item.Tamano,
            Activo = item.Activo,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = item.DeletedAt
        };
        
        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();
    
        return actualizado;
    }

    /// <summary>Obtiene todas las fotos activas o existentes.</summary>
    public async Task<IEnumerable<Foto>> GetAll() {
        logger.LogDebug("Obteniendo todas las Fotos");
        return await context.Set<Foto>()
            .OrderBy(f => f.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Verifica si existe una foto por su nombre.</summary>
    public async Task<bool> ExistById(string key)
    {
        logger.LogDebug("Verificando existencia de Foto por Id: {Id}", key);
        return await context.Set<Foto>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Nombre == key);
    }
}