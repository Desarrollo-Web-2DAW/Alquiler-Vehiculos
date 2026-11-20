using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Alquiler_Vehiculos.Models.Vehiculo;

public record MarcaModel([property: BsonId] ObjectId Id, string Nombre, string Pais, string Web, ICollection<string> ModelosDisponibles, bool IsDeleted, DateTime CreatedAt, DateTime? UpdatedAt, DateTime? DeletedAt );
