using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Services.Vehiculo;
using Alquiler_Vehiculos.Services.Vehiculo.Concesonario;
using Alquiler_Vehiculos.Services.Vehiculo.Marca;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions.Common;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración del catálogo de vehículos: PostgreSQL (vehículos, concesionarios, fotos),
/// MongoDB (marcas), repositorios, servicios y validadores.
/// </summary>
public static class CatalogoConfig {
    /// <summary>
    /// Registra los contextos de datos y servicios del catálogo.
    /// Aplica fail-fast si falta la configuración de conexión.
    /// </summary>
    public static IServiceCollection AddCatalogoServices(this IServiceCollection services, IConfiguration configuration) {
        Log.Information("Configurando catálogo de vehículos...");

        // Catálogo utiliza una base de datos distinta a la de usuarios.
        var catalogoConnectionString = configuration.GetConnectionString("Catalogo");

        if (string.IsNullOrWhiteSpace(catalogoConnectionString)) {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:Catalogo'. " +
                "Añádela en User Secrets o en una variable de entorno.");
        }

        services.AddDbContext<PostgreDbContext>(options => options.UseNpgsql(catalogoConnectionString));

        // Marcas utiliza MongoDB con el driver nativo, sin Entity Framework.
        var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"];
        var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"];

        if (string.IsNullOrWhiteSpace(mongoConnectionString) || string.IsNullOrWhiteSpace(mongoDatabaseName)) {
            throw new InvalidOperationException(
                "Falta configurar 'MongoDbSettings:ConnectionString' o 'MongoDbSettings:DatabaseName'. " +
                "Añádelos en User Secrets o en la configuración del entorno.");
        }

        var mongoClient = new MongoClient(mongoConnectionString);
        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton(mongoClient.GetDatabase(mongoDatabaseName));
        services.AddSingleton<MongoDbContext>();

        services.AddStorage(configuration);

        services.AddScoped<IMarcaRepository, MarcaRepository>();
        services.AddScoped<IMarcaService, MarcaService>();
        services.AddScoped<IVehiculoRepository, VehiculoRepository>();
        services.AddScoped<IVehiculoService, VehiculoService>();
        services.AddScoped<IVehiculoTransactionsService, VehiculoTransactionsService>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddScoped<IConcesionarioRepository, ConcesionarioRepository>();
        services.AddScoped<IConcesionarioService, ConcesionarioService>();
        services.AddScoped<IFotoRepository, FotoRepository>();

        services.AddScoped<CreateVehiculoValidator>();
        services.AddScoped<UpdateVehiculoValidator>();
        services.AddScoped<QueryRequestVehiculoValidator>();
        services.AddScoped<CreateConcesionarioValidator>();
        services.AddScoped<UpdateConcesionarioValidator>();

        Log.Information("Catálogo de vehículos configurado correctamente.");
        return services;
    }

    /// <summary>
    /// Prepara almacenamiento, migraciones/semillas del catálogo e índices de MongoDB.
    /// </summary>
    public static async Task InitializeCatalogoModuleAsync(this WebApplication app) {
        var isDevelopment = app.Environment.IsDevelopment();

        // El almacenamiento se prepara antes de sembrar las fotos.
        app.InitializeStorage(app.Configuration, isDevelopment);
        await app.InitializeCatalogoAsync(isDevelopment);

        app.Services.GetRequiredService<MongoDbContext>().CrearIndices();
    }
}
