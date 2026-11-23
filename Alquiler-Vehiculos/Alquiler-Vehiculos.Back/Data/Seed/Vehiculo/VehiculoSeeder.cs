using Alquiler_Vehiculos.Data.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Storage;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

namespace Alquiler_Vehiculos.Data.Seed.Vehiculo;

/// <summary>
/// Flota inicial: nueve modelos y tres unidades adicionales.
/// Escenario de integración: las semillas de reservas y taller deben completar
/// las relaciones de las matrículas indicadas en BuildVehiculos antes de ofrecer la API.
/// </summary>
public static class VehiculoSeeder {
    
    /// <summary>
    /// Inserta la flota solo si la tabla está vacía. Requiere marcas, concesionarios
    /// y las fotos previamente insertadas por FotoSeeder.
    /// Cada foto se asocia por matrícula, sin recibir rutas manualmente.
    /// No crea el esquema ni borra datos existentes.
    /// </summary>
    public static async Task SeedAsync(PostgreDbContext context, MongoDbContext mongoContext,
        IConfiguration configuration) {
        if (await context.Vehiculos.IgnoreQueryFilters().AnyAsync()) {
            Log.Information("Vehículos: ya existen datos, no se ejecuta la semilla.");
            return;
        }

        var vehiculos = BuildVehiculos(DateTime.UtcNow);
        var uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
        var rutasFotos = vehiculos.Select(v => $"/{uploadPath}/vehiculos/seed-{v.Matrícula}.jpg").ToArray();
        var fotos = await context.Fotos.Where(f => rutasFotos.Contains(f.Ruta) && f.Activo).ToListAsync();
        if (fotos.Count != vehiculos.Length) {
            throw new InvalidOperationException("Faltan fotos de la flota. Ejecuta primero FotoSeeder con las imágenes preparadas.");
        }

        // Se consultan los IDs reales: no se supone que PostgreSQL haya generado del 1 al 5.
        var concesionarios = await context.Concesionarios.IgnoreQueryFilters().ToListAsync();
        var marcas = await mongoContext.Marcas.Find(FilterDefinition<MarcaModel>.Empty).ToListAsync();

        vehiculos = PrepararVehiculos(vehiculos, marcas, concesionarios, fotos, uploadPath);
        context.Vehiculos.AddRange(vehiculos);
        await context.SaveChangesAsync();
        Log.Information("Insertados {Count} vehículos de semilla.", vehiculos.Length);
    }

    // Separar las asociaciones del guardado permite comprobar el catálogo sin bases de datos.
    internal static VehiculoModel[] PrepararVehiculos(VehiculoModel[] vehiculos,
        IReadOnlyCollection<MarcaModel> marcas, IReadOnlyCollection<ConcesionarioModel> concesionarios,
        IReadOnlyCollection<Foto> fotos, string uploadPath) {
        var preparados = new VehiculoModel[vehiculos.Length];
        string[] nombresConcesionarios = [
            "Motor Leganés", "Motor Leganés", "Automóviles Madrid Centro", "Motor Getafe",
            "Vehículos Alcorcón", "Automóviles Madrid Centro", "Motor Fuenlabrada",
            "Motor Leganés", "Motor Getafe", "Vehículos Alcorcón", "Motor Getafe", "Vehículos Alcorcón"
        ];

        for (var i = 0; i < vehiculos.Length; i++) {
            var vehiculo = vehiculos[i];
            var marca = marcas.SingleOrDefault(m => m.Nombre == vehiculo.Marca);
            var concesionario = concesionarios.SingleOrDefault(c => c.Nombre == nombresConcesionarios[i]);
            if (marca is null || !marca.ModelosDisponibles.Contains(vehiculo.Modelo) ||
                concesionario is null ||
                (vehiculo.Activo && (marca.IsDeleted || concesionario.IsDeleted))) {
                throw new InvalidOperationException(
                    $"Faltan datos de catálogo coherentes para {vehiculo.Marca} {vehiculo.Modelo}. Ejecuta primero las semillas de marcas y concesionarios.");
            }

            var rutaFoto = $"/{uploadPath}/vehiculos/seed-{vehiculo.Matrícula}.jpg";
            var foto = fotos.SingleOrDefault(f => f.Ruta == rutaFoto && f.Activo);
            if (foto is null) {
                throw new InvalidOperationException($"Falta la foto activa de la flota para {vehiculo.Matrícula}.");
            }
            preparados[i] = vehiculo with { ConesionarioFk = concesionario.Id, FotoFk = foto.Nombre, Foto = foto };
        }

        return preparados;
    }

    internal static VehiculoModel[] BuildVehiculos(DateTime fecha) {
        // Cifras orientativas para desarrollo: no son fichas técnicas de fabricantes.
        var corolla = new VehiculoModel {
            Matrícula = "1234BCD", Marca = "Toyota", Modelo = "Corolla Sedán", Acriss = "ILAH",
            Carroceria = TipoCarroceria.Sedan, Combustible = TipoCombustible.Hibrido,
            Transmision = TipoTransmision.Automatica, Color = TipoColor.Blanco,
            PrecioBaseDia = 55m, Plazas = 5, MaleteroLitros = 470, AutonomiaKm = 900,
            Kilometraje = 18000, FechaUltimaRevision = fecha.AddMonths(-2), CreatedAt = fecha
        };
        var megane = new VehiculoModel {
            Matrícula = "2345CDF", Marca = "Renault", Modelo = "Megane", Acriss = "CDMD",
            Carroceria = TipoCarroceria.Hatchback, Combustible = TipoCombustible.Diesel,
            Transmision = TipoTransmision.Manual, Color = TipoColor.Azul,
            PrecioBaseDia = 45m, Plazas = 5, MaleteroLitros = 380, AutonomiaKm = 1000,
            Kilometraje = 32000, FechaUltimaRevision = fecha.AddMonths(-1), CreatedAt = fecha
        };
        var transit = new VehiculoModel {
            Matrícula = "9012MNP", Marca = "Ford", Modelo = "Transit", Acriss = "FKMD",
            Carroceria = TipoCarroceria.Furgoneta, Combustible = TipoCombustible.Diesel,
            Transmision = TipoTransmision.Manual, Color = TipoColor.Blanco,
            PrecioBaseDia = 85m, Plazas = 3, MaleteroLitros = 9500, AutonomiaKm = 800,
            Kilometraje = 48000, FechaUltimaRevision = fecha.AddMonths(-1), CreatedAt = fecha
        };

        // Contrato con las futuras semillas del grupo (buscar unidades por matrícula, no por Id aleatorio):
        // 2345CDF: reserva Confirmada viva; vehículo Reservado.
        // 0123NPR: reserva EnCurso; vehículo EnAlquiler.
        // Ambos Megane ocupados permiten preparar el recuento de modelo sin unidades libres.
        // 1357PRS: reserva Confirmada viva; el otro Corolla (1234BCD) sigue Disponible.
        // 9012MNP: parte Abierto, sin reservas vivas; vehículo Mantenimiento.
        // 2468RST: parte Cerrado histórico; Disponible, con cierre igual a FechaUltimaRevision.
        // 7890JKL: Retirado e inactivo, sin reservas vivas ni partes abiertos.
        // Estos estados no sustituyen las reservas/partes: el escenario solo estará completo al integrarlos.
        // Marca conserva el nombre, como el DTO y el mapper actuales del proyecto.
        // Id se deja sin asignar: el generador del contexto crea los 11 caracteres.
        var reservado = megane with { };
        reservado.CambiarEstadoMapper(EstadoVehiculo.Reservado);
        transit.CambiarEstadoMapper(EstadoVehiculo.Mantenimiento);
        var enAlquiler = megane with {
            Matrícula = "0123NPR", Color = TipoColor.Rojo, Kilometraje = 21000, PrecioBaseDia = 48m
        };
        enAlquiler.CambiarEstadoMapper(EstadoVehiculo.EnAlquiler);
        var corollaReservado = corolla with {
            Matrícula = "1357PRS", Color = TipoColor.Negro, Kilometraje = 9000, PrecioBaseDia = 58m
        };
        corollaReservado.CambiarEstadoMapper(EstadoVehiculo.Reservado);
        var disponible = transit with {
            Matrícula = "2468RST", Color = TipoColor.Azul, Kilometraje = 30000, PrecioBaseDia = 90m
        };
        disponible.CambiarEstadoMapper(EstadoVehiculo.Disponible);
        var retirado = new VehiculoModel {
            Matrícula = "7890JKL", Marca = "MINI", Modelo = "Cabrio", Acriss = "MTAV",
            Carroceria = TipoCarroceria.Descapotable, Combustible = TipoCombustible.Gasolina,
            Transmision = TipoTransmision.Automatica, Color = TipoColor.Rojo,
            PrecioBaseDia = 70m, Plazas = 4, MaleteroLitros = 160, AutonomiaKm = 600,
            Kilometraje = 85000, FechaUltimaRevision = fecha.AddMonths(-4), Activo = false,
            CreatedAt = fecha.AddYears(-2), UpdatedAt = fecha, DeletedAt = fecha
        };
        retirado.CambiarEstadoMapper(EstadoVehiculo.Retirado);

        return [
            corolla,
            reservado,
            new VehiculoModel {
                Matrícula = "3456DFG", Marca = "Nissan", Modelo = "Ariya", Acriss = "IFAE",
                Carroceria = TipoCarroceria.Suv, Combustible = TipoCombustible.Electrico,
                Transmision = TipoTransmision.Automatica, Color = TipoColor.Negro,
                PrecioBaseDia = 75m, Plazas = 5, MaleteroLitros = 460, AutonomiaKm = 450,
                Kilometraje = 12000, FechaUltimaRevision = fecha.AddMonths(-2), CreatedAt = fecha
            },
            new VehiculoModel {
                Matrícula = "4567FGH", Marca = "Jeep", Modelo = "Wrangler", Acriss = "FJBD",
                Carroceria = TipoCarroceria.TodoTerreno, Combustible = TipoCombustible.Diesel,
                Transmision = TipoTransmision.Automatica, Color = TipoColor.Verde,
                PrecioBaseDia = 95m, Plazas = 5, MaleteroLitros = 530, AutonomiaKm = 750,
                Kilometraje = 28000, FechaUltimaRevision = fecha.AddMonths(-1), CreatedAt = fecha
            },
            new VehiculoModel {
                Matrícula = "5678GHJ", Marca = "Volkswagen", Modelo = "Passat Variant", Acriss = "IWMD",
                Carroceria = TipoCarroceria.Familiar, Combustible = TipoCombustible.Diesel,
                Transmision = TipoTransmision.Manual, Color = TipoColor.Negro,
                PrecioBaseDia = 65m, Plazas = 5, MaleteroLitros = 650, AutonomiaKm = 1000,
                Kilometraje = 35000, FechaUltimaRevision = fecha.AddMonths(-3), CreatedAt = fecha
            },
            new VehiculoModel {
                Matrícula = "6789HJK", Marca = "BMW", Modelo = "Serie 4 Coupé", Acriss = "PEAV",
                Carroceria = TipoCarroceria.Coupe, Combustible = TipoCombustible.Gasolina,
                Transmision = TipoTransmision.Automatica, Color = TipoColor.Azul,
                PrecioBaseDia = 110m, Plazas = 4, MaleteroLitros = 440, AutonomiaKm = 700,
                Kilometraje = 20000, FechaUltimaRevision = fecha.AddMonths(-2), CreatedAt = fecha
            },
            retirado,
            new VehiculoModel {
                Matrícula = "8901KLM", Marca = "Renault", Modelo = "Grand Scénic", Acriss = "IMMD",
                Carroceria = TipoCarroceria.Monovolumen, Combustible = TipoCombustible.Diesel,
                Transmision = TipoTransmision.Manual, Color = TipoColor.Blanco,
                PrecioBaseDia = 70m, Plazas = 7, MaleteroLitros = 230, AutonomiaKm = 900,
                Kilometraje = 42000, FechaUltimaRevision = fecha.AddMonths(-1), CreatedAt = fecha
            },
            transit,
            enAlquiler,
            corollaReservado,
            disponible
        ];
    }
}
