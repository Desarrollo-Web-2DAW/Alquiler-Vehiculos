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

// Añadimos Middleware
app.UseGlobalExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

app.UseCors("ClientesPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.MapSignalRHubs();

app.Run();