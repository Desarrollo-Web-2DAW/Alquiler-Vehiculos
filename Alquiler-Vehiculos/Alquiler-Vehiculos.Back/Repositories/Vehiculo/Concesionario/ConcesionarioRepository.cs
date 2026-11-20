using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;

/// <summary>
/// Repositorio de Concesionarios que implementa las operaciones CRUD
/// utilizando Entity Framework Core.
/// </summary>
public class ConcesionarioRepository(PostgreDbContext context, ILogger<ConcesionarioRepository> logger) : IConcesionarioRepository
{
    /// <inheritdoc cref="IConcesionarioRepository.Create" />
    public async Task<ConcesionarioModel> Create(ConcesionarioModel item) {
        logger.LogInformation("Creando Concesionario: {nombre}", item.Nombre);
        await context.Concesionarios.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }
    
    /// <inheritdoc cref="IConcesionarioRepository.SoftDelete" />
    public async Task<bool> SoftDelete(long key) {
        logger.LogInformation("Realizando borrado lógico de Concesionario ID: {Id}", key);
        return await context.Concesionarios
            .Where(f => f.Id == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)) > 0;
    }

    /// <inheritdoc cref="IConcesionarioRepository.GetById" />
    public async Task<ConcesionarioModel?> GetById(long key) {
        logger.LogDebug("Obteniendo Concesionario por ID: {Key}", key);
        return await context.Concesionarios
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IConcesionarioRepository.Update" />
    public async Task<ConcesionarioModel?> Update(long key, ConcesionarioModel item) 
    {
        logger.LogInformation("Actualizando Concesionario ID: {Id}", key);
    
        ConcesionarioModel? existente = await context.Concesionarios.FirstOrDefaultAsync(f => f.Id == key);
        if (existente == null || existente.IsDeleted) return null;
        
        var actualizado = existente with {
            Nombre = item.Nombre,
            Ciudad = item.Ciudad,
            Telefono = item.Telefono,
            Email = item.Email,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = item.DeletedAt
        };
        
        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();
    
        return actualizado;
    }

    /// <inheritdoc cref="IConcesionarioRepository.GetAll" />
    public async Task<IEnumerable<ConcesionarioModel>> GetAll() {
        logger.LogDebug("Obteniendo todos los Concesionarios");
        return await context.Concesionarios
            .OrderBy(f => f.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc cref="IConcesionarioRepository.ExistById" />
    public async Task<bool> ExistById(long key)
    {
        logger.LogDebug("Verificando existencia de Concesionario por Id: {Id}", key);
        return await context.Concesionarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Id == key);
    }
}
