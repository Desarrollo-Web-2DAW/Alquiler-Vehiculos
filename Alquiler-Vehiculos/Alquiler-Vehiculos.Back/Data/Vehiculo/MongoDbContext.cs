using Alquiler_Vehiculos.Models.Vehiculo;
using MongoDB.Driver;

namespace Alquiler_Vehiculos.Data.Vehiculo;

public class MongoDbContext(IMongoDatabase database){
    public IMongoCollection<Marca> Marcas =>database.GetCollection<Marca>("marcas");
    
    
    public void CrearIndices() {
        var indiceNombre = new CreateIndexModel<Marca>(Builders<Marca>.IndexKeys.Ascending(m => m.Nombre));
        Marcas.Indexes.CreateOne(indiceNombre);
    }
}