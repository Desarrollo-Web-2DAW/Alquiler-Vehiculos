using Alquiler_Vehiculos.Models.Vehiculo;
using MongoDB.Driver;

namespace Alquiler_Vehiculos.Data.Vehiculo;

/// <summary>
/// Acceso a las marcas del catálogo mediante el driver nativo de MongoDB.
/// No hereda de DbContext ni utiliza las migraciones de Entity Framework Core.
/// </summary>
/// <param name="database">Base de datos MongoDB configurada para el catálogo.</param>
public class MongoDbContext(IMongoDatabase database){
    /// <summary>Colección de marcas y sus modelos disponibles.</summary>
    public IMongoCollection<MarcaModel> Marcas => database.GetCollection<MarcaModel>("marcas");
    
    
    /// <summary>
    /// Crea el índice ascendente no único sobre el nombre de la marca.
    /// Si ya existe con la misma definición, lo conserva sin borrar documentos.
    /// </summary>
    public void CrearIndices() {
        var indiceNombre = new CreateIndexModel<MarcaModel>(Builders<MarcaModel>.IndexKeys.Ascending(m => m.Nombre));
        Marcas.Indexes.CreateOne(indiceNombre);
    }
}
