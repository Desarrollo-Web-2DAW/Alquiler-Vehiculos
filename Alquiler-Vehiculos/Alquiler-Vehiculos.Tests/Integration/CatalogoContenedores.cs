using Npgsql;
using Testcontainers.MongoDb;

namespace Alquiler_Vehiculos.Tests.Integration;

/// <summary>
/// Infraestructura del catálogo para las pruebas que levantan la API completa con
/// <c>WebApplicationFactory</c>: la aplicación exige la base de datos del catálogo (PostgreSQL)
/// y MongoDB (marcas) para arrancar.
/// </summary>
/// <remarks>
/// El catálogo usa una base de datos propia dentro del mismo contenedor PostgreSQL de usuarios;
/// las migraciones del catálogo la crean al arrancar.
/// </remarks>
internal sealed class CatalogoContenedores : IAsyncDisposable {
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:7.0").Build();
    private string? _catalogoConnectionString;

    /// <summary>Arranca MongoDB y prepara la cadena de conexión del catálogo.</summary>
    /// <param name="usuariosConnectionString">Cadena del contenedor PostgreSQL de usuarios.</param>
    public async Task StartAsync(string usuariosConnectionString) {
        await _mongo.StartAsync();
        _catalogoConnectionString = new NpgsqlConnectionStringBuilder(usuariosConnectionString) {
            Database = "alquiler_catalogo_test"
        }.ConnectionString;
    }

    /// <summary>
    /// Variables de entorno que lee <c>Program</c> antes de construir el host
    /// (la configuración en memoria de la factoría llega tarde para los registros fail-fast).
    /// </summary>
    public Dictionary<string, string?> VariablesDeEntorno() => new() {
        ["ConnectionStrings__Catalogo"] = _catalogoConnectionString,
        ["MongoDbSettings__ConnectionString"] = _mongo.GetConnectionString(),
        ["MongoDbSettings__DatabaseName"] = "catalogo_test"
    };

    public async ValueTask DisposeAsync() => await _mongo.DisposeAsync();
}
