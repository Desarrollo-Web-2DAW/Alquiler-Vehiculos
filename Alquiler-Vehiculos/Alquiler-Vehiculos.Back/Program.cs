using System.Text;
using Alquiler_Vehiculos.Cache;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Middleware;
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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Caché LRU con TTL sobre Redis (carpeta Cache/)
builder.Services.AddCache(builder.Configuration);

// Notificaciones por correo (RF-34 a RF-36): cola de envío con reintentos en segundo plano
builder.Services.AddEmailNotifications(builder.Configuration);

// Partes de taller (RF-08): núcleo del taller
builder.Services.AddTallerServices();

var app = builder.Build();

// Añadimos Middleware
app.UseGlobalExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

app.Run();