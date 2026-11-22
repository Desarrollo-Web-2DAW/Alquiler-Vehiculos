using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;

/// <summary>
/// Repositorio de Vehiculos que implementa las operaciones CRUD
/// utilizando Entity Framework Core.
/// </summary>
public class VehiculoRepository(PostgreDbContext context, ILogger<VehiculoRepository> logger) : IVehiculoRepository
{
    /// <inheritdoc cref="IVehiculoRepository.Create" />
    public async Task<VehiculoModel> Create(VehiculoModel item)
    {
        logger.LogInformation("Creando Vehiculo: {matricula}", item.Matrícula);
        await context.Vehiculos.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc cref="IVehiculoRepository.SoftDelete" />
    public async Task<bool> SoftDelete(string key)
    {
        logger.LogInformation("Realizando borrado lógico de Vehiculo ID: {Id}", key);

        VehiculoModel? existente = await context.Vehiculos.FirstOrDefaultAsync(f => f.Id == key);
        if (existente == null) return false;

        // Se actualiza la instancia rastreada (no ExecuteUpdate) para que los cambios
        // queden reflejados también en el DbContext y en la transacción en curso.
        var actualizado = existente with
        {
            Activo = false,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        };
        // RF-01: la baja lógica deja el vehículo Retirado y con Activo = false a la vez.
        actualizado.CambiarEstadoMapper(EstadoVehiculo.Retirado);

        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc cref="IVehiculoRepository.GetById" />
    public async Task<VehiculoModel?> GetById(string key)
    {
        logger.LogDebug("Obteniendo Vehiculo por ID: {Key}", key);
        return await context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Foto)
            .FirstOrDefaultAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IVehiculoRepository.Update" />
    public async Task<VehiculoModel?> Update(string key, VehiculoModel item)
    {
        logger.LogInformation("Actualizando Vehiculo ID: {Id}", key);

        VehiculoModel? existente = await context.Vehiculos.FirstOrDefaultAsync(f => f.Id == key);
        if (existente == null || !existente.Activo) return null;

        var actualizado = existente with
        {
            FotoFk = item.FotoFk,
            Foto = item.Foto,
            Matrícula = item.Matrícula,
            Marca = item.Marca,
            Modelo = item.Modelo,
            Acriss = item.Acriss,
            Carroceria = item.Carroceria,
            Combustible = item.Combustible,
            Transmision = item.Transmision,
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

        actualizado.CambiarEstadoMapper(item.Estado);

        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();

        return actualizado;
    }

    /// <inheritdoc cref="IVehiculoRepository.GetAll" />
    public async Task<IEnumerable<VehiculoModel>> GetAll()
    {
        logger.LogDebug("Obteniendo todos los Vehiculos");
        return await context.Vehiculos.OrderBy(f => f.Matrícula)
            .AsNoTracking()
            .Include(f => f.Foto)
            .ToListAsync();
    }

    /// <summary>Recupera una página de Vehiculos con su categoría y el total de registros.</summary>
    public async Task<(IEnumerable<VehiculoModel> Items, int TotalItems)> GetPaged(int page, int pageSize)
    {
        logger.LogDebug("Obteniendo página {Page} de Vehiculos con tamaño {PageSize}", page, pageSize);
        var query = context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Foto)
            .OrderBy(f => f.Matrícula);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc cref="IVehiculoRepository.GetByMatricula" />
    public async Task<VehiculoModel?> GetByMatricula(string matricula)
    {
        logger.LogDebug("Obteniendo Vehiculo por matricula: {matricula}", matricula);
        return await context.Vehiculos
            .AsNoTracking()
            .Include(f => f.Foto)
            .FirstOrDefaultAsync(f => f.Matrícula == matricula);
    }

    /// <inheritdoc cref="IVehiculoRepository.ExistByMatricula" />
    public async Task<bool> ExistByMatricula(string matricula)
    {
        logger.LogDebug("Verificando existencia de Vehiculo por matricula: {Matricula}", matricula);
        return await context.Vehiculos
            // El índice único reserva también los nombres de los eliminados.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Matrícula == matricula);
    }

    /// <inheritdoc cref="IVehiculoRepository.ExistById" />
    public async Task<bool> ExistById(string key)
    {
        logger.LogDebug("Verificando existencia de Vehiculo por Id: {Id}", key);
        return await context.Vehiculos
            // El índice único reserva también los nombres de los eliminados.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IVehiculoRepository.Restore" />
    public async Task<VehiculoModel?> Restore(string key)
    {
        logger.LogDebug("Restaurando vehiculo por Id: {Id}", key);

        VehiculoModel? existente = await context.Set<VehiculoModel>().FirstOrDefaultAsync(f => f.Id == key);
        if (existente == null) return null;

        var actualizado = existente with
        {
            Activo = true,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        // Simétrico a SoftDelete: al reactivarlo vuelve a estar Disponible.
        actualizado.CambiarEstadoMapper(EstadoVehiculo.Disponible);

        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();

        return actualizado;
    }
}