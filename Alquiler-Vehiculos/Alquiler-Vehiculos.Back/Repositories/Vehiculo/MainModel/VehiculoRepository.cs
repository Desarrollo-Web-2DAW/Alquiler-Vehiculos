using Alquiler_Vehiculos.Models.Vehiculo;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;

/// <summary>
/// Repositorio de Vehiculos que implementa las operaciones CRUD
/// utilizando Entity Framework Core.
/// </summary>
public class VehiculoRepository(AppDbContext context, ILogger<VehiculoRepository> logger) : IVehiculoRepository
{
    /// <inheritdoc cref="IVehiculoRepository.Create" />
    public async Task<VehiculoModel> Create(VehiculoModel item) {
        logger.LogInformation("Creando Vehiculo: {matricula}", item.Matrícula);
        await context.Vehiculos.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }
    
    /// <inheritdoc cref="IVehiculoRepository.SoftDelete" />
    public async Task<bool> SoftDelete(string key) {
        logger.LogInformation("Realizando borrado lógico de Vehiculo ID: {Id}", key);
        return await context.Vehiculos
            .Where(f => f.Id == key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)) > 0;
    }

    /// <inheritdoc cref="IVehiculoRepository.GetById" />
    public async Task<VehiculoModel?> GetById(string key) {
        logger.LogDebug("Obteniendo Vehiculo por ID: {Key}", key);
        return await context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IVehiculoRepository.Update" />
    public async Task<VehiculoModel?> Update(string key, VehiculoModel item) 
    {
        logger.LogInformation("Actualizando Vehiculo ID: {Id}", key);
    
        VehiculoModel existente = await context.Vehiculos.FirstOrDefaultAsync(f => f.Id == key);
        if (existente == null || existente.Activo) return null;
        
        var actualizado = existente with {
            FotoFk = item.FotoFk,
            Foto = item.Foto,
            Matrícula = item.Matrícula,
            Marca = item.Marca,
            Modelo = item.Modelo,
            Acriss = item.Acriss,
            Carroceria = item.Carroceria,
            Combustible = item.Combustible,
            Transmision = item.Transmision,
            Estado = item.Estado,
            Color = item.Color,
            PrecioBaseDia = item.PrecioBaseDia,
            Plazas = item.Plazas,
            MaleteroLitros = item.MaleteroLitros,
            AutonomiaKm = item.AutonomiaKm,
            Kilometraje = item.Kilometraje,
            FechaUltimaRevision = item.FechaUltimaRevision,
            ConesionarioFk = item.ConesionarioFk,
            Activo = item.Activo,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = item.DeletedAt
        };
        
        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();
    
        return actualizado;
    }

    /// <inheritdoc cref="IVehiculoRepository.GetAll" />
    public async Task<IEnumerable<VehiculoModel>> GetAll() {
        logger.LogDebug("Obteniendo todos los Vehiculos");
        return await context.Vehiculos.OrderBy(f => f.matricula)
            .AsNoTracking()
            .Include(f => f.Categoria)
            .ToListAsync();
    }

    /// <summary>Recupera una página de Vehiculos con su categoría y el total de registros.</summary>
    public async Task<(IEnumerable<VehiculoModel> Items, int TotalItems)> GetPaged(int page, int pageSize) {
        logger.LogDebug("Obteniendo página {Page} de Vehiculos con tamaño {PageSize}", page, pageSize);
        var query = context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .OrderBy(f => f.matricula);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc cref="IVehiculoRepository.GetByMatricula" />
    public async Task<VehiculoModel?> GetByMatricula(string matricula) {
        logger.LogDebug("Obteniendo Vehiculo por matricula: {matricula}", matricula);
        return await context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.matricula == matricula);
    }

    /// <inheritdoc cref="IVehiculoRepository.ExistByMatricula" />
    public async Task<bool> ExistByMatricula(string matricula) {
        logger.LogDebug("Verificando existencia de Vehiculo por matricula: {Matricula}", matricula);
        return await context.Vehiculos
            // El índice único reserva también los nombres de los eliminados.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.matricula == matricula);
    }

    /// <inheritdoc cref="IVehiculoRepository.ExistById" />
    public async Task<bool> ExistById(string key)
    {
        logger.LogDebug("Verificando existencia de Vehiculo por Id: {Id}", key);
        return await context.Vehiculos
            // El índice único reserva también los nombres de los eliminados.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.matricula == key);
    }
}