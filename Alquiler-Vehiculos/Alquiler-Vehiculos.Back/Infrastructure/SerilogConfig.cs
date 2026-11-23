using Serilog;
using Serilog.Events;

namespace Alquiler_Vehiculos.Infrastructure;

/// <summary>
/// Configuración centralizada de Serilog (RNF-09).
/// Filtra el ruido de EF Core y Npgsql para dejar la consola y el fichero legibles.
/// </summary>
public static class SerilogConfig {
    /// <summary>
    /// Construye la configuración del logger con salida a consola y fichero diario.
    /// </summary>
    public static LoggerConfiguration Configure() {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
            .MinimumLevel.Override("Npgsql", LogEventLevel.Error)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    }
}
