using Alquiler_Vehiculos.Models.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alquiler_Vehiculos.Repositories.Storage;

/// <summary>
/// Repositorio de Fotos que implementa las operaciones CRUD
/// utilizando Entity Framework Core, usando el Nombre como clave primaria.
/// </summary>
public class FotoRepository(AppDbContext context, ILogger<FotoRepository> logger) : IFotoRepository
{
    /// <inheritdoc cref="IFotoRepository.Create" />
    public async Task<Foto> Create(Foto item) {
        logger.LogInformation("Creando Foto: {nombre}", item.Nombre);
        await context.Set<Foto>().AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }
    
    /// <inheritdoc cref="IFotoRepository.SoftDelete" />
    public async Task<bool> SoftDelete(string key) {
        logger.LogInformation("Realizando borrado lógico de Foto ID: {Id}", key);
        return await context.Set<Foto>()
            .Where(f => f.Nombre == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.DeletedAt, DateTime.UtcNow)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)
                .SetProperty(f => f.Activo, false)) > 0;
    }

    /// <inheritdoc cref="IFotoRepository.GetById" />
    public async Task<Foto?> GetById(string key) {
        logger.LogDebug("Obteniendo Foto por ID: {Key}", key);
        return await context.Set<Foto>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Nombre == key);
    }

    /// <inheritdoc cref="IFotoRepository.Update" />
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

    /// <inheritdoc cref="IFotoRepository.GetAll" />
    public async Task<IEnumerable<Foto>> GetAll() {
        logger.LogDebug("Obteniendo todas las Fotos");
        return await context.Set<Foto>()
            .OrderBy(f => f.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc cref="IFotoRepository.ExistById" />
    public async Task<bool> ExistById(string key)
    {
        logger.LogDebug("Verificando existencia de Foto por Id: {Id}", key);
        return await context.Set<Foto>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Nombre == key);
    }

    /// <inheritdoc cref="IFotoRepository.Restore" />
    public async Task<Foto?> Restore(string name)
    {
        logger.LogDebug("Restaurando Foto por Id: {Id}", name);
        
        Foto existente = await context.Set<Foto>().FirstOrDefaultAsync(f => f.Nombre == name);
        if (existente == null) return null;
        
        var actualizado = existente with {
            Activo = true,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        
        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();
    
        return actualizado;
    }
}