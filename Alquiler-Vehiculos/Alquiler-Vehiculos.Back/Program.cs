using System.Text;
using Alquiler_Vehiculos.Cache;
<<<<<<< HEAD
=======
using Alquiler_Vehiculos.Config;
>>>>>>> dev
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Middleware;
using FluentValidation;
using Serilog;

Console.OutputEncoding = Encoding.UTF8;
Log.Logger = SerilogConfig.Configure().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Log.Logger);

try {
    Log.Information("Iniciando la aplicación...");

    builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));
    builder.Services.AddOpenApi();
    builder.Services.AddDatabase(builder.Configuration, builder.Environment);
    builder.Services.AddIdentityConfiguration(builder.Configuration, builder.Environment);
    builder.Services.AddControllers();
    builder.Services.AddHealthChecks();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

<<<<<<< HEAD
// Configuración de Identity, JWT y autorización.
builder.Services.AddIdentityConfiguration(builder.Configuration, builder.Environment);

// Agregamos SignalR
builder.Services.AddRealtimeSignalR();

builder.Services.AddCors(options => {
    options.AddPolicy("ClientesPolicy", policy => {
        policy.WithOrigins("https://localhost:7150")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
=======
    builder.Services.AddCache(builder.Configuration);
    builder.Services.AddEmailNotifications(builder.Configuration);
    builder.Services.AddTallerServices();
>>>>>>> dev

    var app = builder.Build();

    app.UseGlobalExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment()) {
        app.MapOpenApi();
    } else if (!app.Environment.IsEnvironment("Testing")) {
        app.UseHttpsRedirection();
    }

<<<<<<< HEAD
app.UseCors("ClientesPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.MapSignalRHubs();

app.Run();
=======
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    await app.InitializeDatabaseAsync();

    await app.RunAsync();
} catch (HostAbortedException) {
    // Normal al ejecutar `dotnet ef`.
} catch (Exception ex) {
    Log.Fatal(ex, "La aplicación terminó de forma inesperada");
} finally {
    await Log.CloseAndFlushAsync();
}

public partial class Program;
>>>>>>> dev
