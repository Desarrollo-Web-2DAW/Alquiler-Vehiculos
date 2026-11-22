using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Repositories.Vehiculo.Marca;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using Alquiler_Vehiculos.Repositories.Vehiculo.Concesionario;
using Alquiler_Vehiculos.Repositories.Storage;
using Alquiler_Vehiculos.Services.Vehiculo;
using Alquiler_Vehiculos.Services.Vehiculo.Concesonario;
using Alquiler_Vehiculos.Services.Vehiculo.Transactions;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Validators.Catalogo_Vehiculos.Concesionario;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using FluentValidation;
using Alquiler_Vehiculos.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;

Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Logging.AddSerilog(Log.Logger);
builder.Host.UseSerilog(Log.Logger);

try {
    Log.Information("Iniciando la aplicación...");

    // ── CONFIGURACIÓN TIPADA ─────────────────────────────────────────────────
    builder.Services.Configure<ApiSettings>(
        builder.Configuration.GetSection("Api"));

    // ── BASE DE DATOS ────────────────────────────────────────────────────────
    // Fail-fast: sin cadena de conexión la API no debe arrancar.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString)) {
        throw new InvalidOperationException(
            "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. " +
            "Añádela en appsettings.Development.json o en User Secrets.");
    }

    builder.Services.AddDbContext<AppDbContextUsuarios>(options =>
        options.UseNpgsql(connectionString));

    // Catálogo utiliza una base de datos distinta a la de usuarios.
    var catalogoConnectionString = builder.Configuration.GetConnectionString("Catalogo");

    if (string.IsNullOrWhiteSpace(catalogoConnectionString)) {
        throw new InvalidOperationException(
            "Falta la cadena de conexión 'ConnectionStrings:Catalogo'. " +
            "Añádela en User Secrets o en una variable de entorno.");
    }

    builder.Services.AddDbContext<PostgreDbContext>(options =>
        options.UseNpgsql(catalogoConnectionString));

    // Marcas utiliza MongoDB con el driver nativo, sin Entity Framework.
    var mongoConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
    var mongoDatabaseName = builder.Configuration["MongoDbSettings:DatabaseName"];

    if (string.IsNullOrWhiteSpace(mongoConnectionString) ||
        string.IsNullOrWhiteSpace(mongoDatabaseName)) {
        throw new InvalidOperationException(
            "Falta configurar 'MongoDbSettings:ConnectionString' o " +
            "'MongoDbSettings:DatabaseName'. Añádelos en User Secrets " +
            "o en la configuración del entorno.");
    }

    var mongoClient = new MongoClient(mongoConnectionString);
    var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);

    builder.Services.AddSingleton<IMongoClient>(mongoClient);
    builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
    builder.Services.AddSingleton<MongoDbContext>();

    // ── IDENTITY + JWT ─────────────────────────────────────────────────────
    builder.Services.AddIdentityConfiguration(builder.Configuration, builder.Environment);

    // ── CONTROLADORES Y VALIDACIÓN ───────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor(); // SignInManager lo necesita
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddCache(builder.Configuration);
    builder.Services.AddStorage(builder.Configuration);
    // ── SERVICIOS ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
    builder.Services.AddScoped<IMarcaRepository, MarcaRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IVehiculoRepository, VehiculoRepository>();
    builder.Services.AddScoped<IVehiculoService, VehiculoService>();
    builder.Services.AddScoped<IVehiculoTransactionsService, VehiculoTransactionsService>();
    builder.Services.AddScoped<IConcesionarioRepository, ConcesionarioRepository>();
    builder.Services.AddScoped<IConcesionarioService, ConcesionarioService>();
    builder.Services.AddScoped<IFotoRepository, FotoRepository>();
    builder.Services.AddScoped<CreateVehiculoValidator>();
    builder.Services.AddScoped<UpdateVehiculoValidator>();
    builder.Services.AddScoped<CreateConcesionarioValidator>();
    builder.Services.AddScoped<UpdateConcesionarioValidator>();

    // ── PIPELINE DE MIDDLEWARES ───────────────────────────────────────────────
    var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}
    // Crear el índice de marcas al arrancar, sin borrar datos de MongoDB.
    var mongoContext = app.Services.GetRequiredService<MongoDbContext>();
    mongoContext.CrearIndices();

    app.UseSerilogRequestLogging();

    //app.UseGlobalExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment()) {
       // app.UseSwagger();
       // app.UseSwaggerUI();
    }

    app.MapControllers();

    // ── BASE DE DATOS: esquema + roles y cuentas iniciales ───────────────────
    // El esquema se construye SIEMPRE con migraciones, nunca con EnsureCreated.
    // Las migraciones van antes del seeder: sin tablas, el seeder no puede
    // escribir roles ni cuentas.
    using (var scope = app.Services.CreateScope()) {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextUsuarios>();

        // Aplicar las migraciones pendientes sin borrar los datos existentes.
        await db.Database.MigrateAsync();
    }

    await IdentitySeeder.SeedAsync(app.Services, app.Configuration);

    // ── CONFIGURACIÓN TIPADA: se lee y se muestra al arrancar ────────────────
    var apiConfig = app.Services.GetRequiredService<IOptions<ApiSettings>>().Value;

    Log.Information("API {Nombre}: {Mensaje}", apiConfig.Nombre, apiConfig.Mensaje);

    var port = builder.Configuration["ASPNETCORE_URLS"]?.Split(':').LastOrDefault() ?? "5000";
    Console.WriteLine("========================================");
    Console.WriteLine($"  {apiConfig.Nombre}");
    Console.WriteLine($"  {apiConfig.Mensaje}");
    Console.WriteLine($"  http://localhost:{port}");
    Console.WriteLine($"  Entorno: {app.Environment.EnvironmentName}");
    Console.WriteLine("========================================");

    app.UseHttpsRedirection();

    await app.RunAsync();
} catch (HostAbortedException) {
    // Las herramientas de EF Core abortan el host a propósito al leer el modelo
    // para generar una migración. No es un error: solo ocurre con `dotnet ef`.
} catch (Exception ex) {
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
} finally {
    await Log.CloseAndFlushAsync();
}
