using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Alquiler_Vehiculos.Repositories.Vehiculo.Marca;

/// <summary>
/// Repositorio de marcas que utiliza el driver nativo de MongoDB.
/// Las consultas habituales excluyen las marcas dadas de baja, pero sus documentos
/// se conservan para mantener la trazabilidad exigida por RF-09.
/// </summary>
/// <param name="context">Contexto que proporciona la colección de marcas.</param>
/// <param name="logger">Registrador de las operaciones del repositorio.</param>
public class MarcaRepository(MongoDbContext context, ILogger<MarcaRepository> logger) : IMarcaRepository {
    
    /// <summary>Crea una marca activa; el driver genera el ObjectId si está vacío.</summary>
    /// <param name="item">Marca que se desea guardar.</param>
    /// <returns>Marca guardada con su ObjectId y la fecha de creación en UTC.</returns>
    public async Task<MarcaModel> Create(MarcaModel item) {
        logger.LogInformation("Creando marca: {Nombre}", item.Nombre);
        var nueva = item with {
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            DeletedAt = null
        };
        await context.Marcas.InsertOneAsync(nueva);
        return nueva;
    }

    /// <inheritdoc cref="IMarcaRepository.GetById"/>
    public async Task<MarcaModel?> GetById(string key) {
        logger.LogDebug("Obteniendo marca por ID: {Key}", key);
        if (!ObjectId.TryParse(key, out var objectId)) return null;
        return await context.Marcas
            .Find(m => m.Id == objectId && !m.IsDeleted)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IMarcaRepository.GetAll"/>
    public async Task<IEnumerable<MarcaModel>> GetAll() {
        logger.LogDebug("Obteniendo todas las marcas activas");
        return await context.Marcas
            .Find(m => !m.IsDeleted)
            .SortBy(m => m.Nombre)
            .ToListAsync();
    }

    /// <inheritdoc cref="IMarcaRepository.Update"/>
    public async Task<MarcaModel?> Update(string key, MarcaModel item) {
        logger.LogInformation("Actualizando marca ID: {Id}", key);
        var existente = await GetById(key);
        if (existente is null) return null;

        var actualizada = existente with {
            Nombre = item.Nombre,
            Pais = item.Pais,
            Web = item.Web,
            ModelosDisponibles = item.ModelosDisponibles,
            UpdatedAt = DateTime.UtcNow
        };

        var resultado = await context.Marcas.ReplaceOneAsync(m => m.Id == existente.Id && !m.IsDeleted, actualizada);
        return resultado.MatchedCount > 0 ? actualizada : null;
    }

    /// <inheritdoc cref="IMarcaRepository.SoftDelete"/>
    public async Task<bool> SoftDelete(string key) {
        logger.LogInformation("Realizando borrado lógico de marca ID: {Id}", key);
        if (!ObjectId.TryParse(key, out var objectId)) return false;
        var fecha = DateTime.UtcNow;
        var resultado = await context.Marcas.UpdateOneAsync(m => m.Id == objectId && !m.IsDeleted,
            Builders<MarcaModel>.Update
                .Set(m => m.IsDeleted, true)
                .Set(m => m.UpdatedAt, fecha)
                .Set(m => m.DeletedAt, fecha));
        return resultado.ModifiedCount > 0;
    }

    /// <inheritdoc cref="IMarcaRepository.ExistById"/>
    public async Task<bool> ExistById(string key) {
        logger.LogDebug("Verificando existencia de marca por ID: {Key}", key);
        if (!ObjectId.TryParse(key, out var objectId)) return false;
        return await context.Marcas
            .Find(m => m.Id == objectId && !m.IsDeleted)
            .AnyAsync();
    }
}
