using System.Text;
using Alquiler_Vehiculos.Config;
using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Repositories.Clientes;
using Alquiler_Vehiculos.Services.Usuarios.Auth;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

    // ── IDENTITY + JWT ─────────────────────────────────────────────────────
    builder.Services.AddIdentityConfiguration(builder.Configuration, builder.Environment);

    // ── CONTROLADORES Y VALIDACIÓN ───────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor(); // SignInManager lo necesita
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ── SERVICIOS ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // ── PIPELINE DE MIDDLEWARES ───────────────────────────────────────────────
    var app = builder.Build();

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

        // En desarrollo se parte de cero en cada arranque (criterio del profe).
        if (app.Environment.IsDevelopment()) {
            await db.Database.EnsureDeletedAsync();
        }

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
    if (app.Environment.IsDevelopment()) {
        Console.WriteLine($"  Swagger: http://localhost:{port}/swagger");
        Console.WriteLine($"  GraphQL: http://localhost:{port}/graphql");
    }
    Console.WriteLine($"  Entorno: {app.Environment.EnvironmentName}");
    Console.WriteLine("========================================");

    app.UseHttpsRedirection();

    var summaries = new[] {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () => {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    (
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

    await app.RunAsync();
} catch (HostAbortedException) {
    // Las herramientas de EF Core abortan el host a propósito al leer el modelo
    // para generar una migración. No es un error: solo ocurre con `dotnet ef`.
} catch (Exception ex) {
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
} finally {
    await Log.CloseAndFlushAsync();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) {
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
