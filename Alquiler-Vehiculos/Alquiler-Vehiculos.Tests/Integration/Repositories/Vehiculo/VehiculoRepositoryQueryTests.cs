using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Repositories.Vehiculo.MainModel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

public partial class VehiculoRepositoryTests {
    [TestCase("Marca", "oYo", "1234BCD,4567JKL")]
    [TestCase("Marca", "NoExiste", "")]
    [TestCase("Marca", "%", "")]
    [TestCase("Modelo", "oRoL", "1234BCD,4567JKL")]
    [TestCase("Modelo", "_", "")]
    [TestCase("Categoria", "sEdAn", "1234BCD,4567JKL")]
    [TestCase("Categoria", "hatchback", "2345CDF")]
    [TestCase("Estado", "disponible", "1234BCD")]
    [TestCase("Estado", "reservado", "2345CDF")]
    [TestCase("Estado", "mantenimiento", "3456FGH")]
    [TestCase("Estado", "retirado", "4567JKL")]
    [TestCase("Combustible", "hibrido", "1234BCD,4567JKL")]
    [TestCase("Combustible", "diesel", "2345CDF")]
    [TestCase("Combustible", "electrico", "3456FGH")]
    [TestCase("Transmision", "manual", "2345CDF")]
    [TestCase("Transmision", "automatica", "1234BCD,3456FGH,4567JKL")]
    [TestCase("PlazasMinimas", "5", "1234BCD,2345CDF,3456FGH,4567JKL")]
    [TestCase("PlazasMinimas", "6", "3456FGH")]
    [TestCase("PlazasMinimas", "8", "")]
    [TestCase("MaleteroLitrosMinimos", "0", "1234BCD,2345CDF,3456FGH,4567JKL")]
    [TestCase("MaleteroLitrosMinimos", "400", "2345CDF,3456FGH")]
    [TestCase("MaleteroLitrosMinimos", "501", "")]
    [TestCase("AutonomiaKmMinima", "700", "1234BCD,4567JKL")]
    [TestCase("AutonomiaKmMinima", "701", "")]
    public async Task GetByFilters_FiltroIndividual_FiltraEnPostgreSql(string campo, string valor, string matriculas) {
        // Arrange
        await GuardarFlotaQuery();
        var request = CrearFiltro(campo, valor);
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = (await repository.GetByFilters(request)).ToList();

        // Assert
        resultado.Select(v => v.Matrícula).Should().Equal(matriculas.Split(',', StringSplitOptions.RemoveEmptyEntries));
        db.ChangeTracker.Entries().Should().BeEmpty();
        resultado.Should().OnlyContain(v => v.Foto == null, "el recuento no necesita cargar las fotografías");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetByFilters_SinFiltrosNulosOVacios_ConservaOcupadosYRetirados(bool vacios) {
        // Arrange
        await GuardarFlotaQuery();
        var request = vacios ? new QueryRequestVehiculoDto {
            Marca = "", Modelo = "", Categoria = "", Estado = "", Combustible = "", Transmision = ""
        } : new QueryRequestVehiculoDto();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = (await repository.GetByFilters(request)).ToList();

        // Assert
        resultado.Select(v => v.Matrícula).Should().Equal("1234BCD", "2345CDF", "3456FGH", "4567JKL");
        resultado.Should().Contain(v => v.Estado == EstadoVehiculo.Reservado);
        resultado.Should().Contain(v => v.Estado == EstadoVehiculo.Retirado && !v.Activo);
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByFilters_FiltrosCombinados_DebenCumplirseTodos(bool compatibles) {
        // Arrange
        await GuardarFlotaQuery();
        var request = compatibles ? new QueryRequestVehiculoDto {
            Marca = "REn", Modelo = "eGa", Categoria = "Hatchback", Estado = "Reservado",
            Combustible = "Diesel", Transmision = "Manual", PlazasMinimas = 5,
            MaleteroLitrosMinimos = 400, AutonomiaKmMinima = 600
        } : new QueryRequestVehiculoDto { Categoria = "Sedan", Combustible = "Diesel" };
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.GetByFilters(request);

        // Assert
        resultado.Select(v => v.Matrícula).Should().Equal(compatibles ? new[] { "2345CDF" } : Array.Empty<string>());
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [TestCase("Categoria", "0")]
    [TestCase("Categoria", "Sedan, Suv")]
    [TestCase("Categoria", "999")]
    [TestCase("Categoria", "NoExiste")]
    [TestCase("Estado", "0")]
    [TestCase("Estado", "Disponible, Reservado")]
    [TestCase("Estado", "999")]
    [TestCase("Estado", "NoExiste")]
    [TestCase("Combustible", "0")]
    [TestCase("Combustible", "Gasolina, Diesel")]
    [TestCase("Combustible", "999")]
    [TestCase("Combustible", "NoExiste")]
    [TestCase("Transmision", "0")]
    [TestCase("Transmision", "Manual, Automatica")]
    [TestCase("Transmision", "999")]
    [TestCase("Transmision", "NoExiste")]
    public async Task GetByFilters_ClasificacionInvalidaDirecta_DevuelveVacio(string campo, string valor) {
        // Arrange
        await GuardarFlotaQuery();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var request = CrearFiltro(campo, valor);

        // Act
        var resultado = await repository.GetByFilters(request);

        // Assert
        resultado.Should().BeEmpty();
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Test]
    public async Task GetByFilters_BaseVacia_DevuelveVacio() {
        // Arrange
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);

        // Act
        var resultado = await repository.GetByFilters(new QueryRequestVehiculoDto());

        // Assert
        resultado.Should().BeEmpty();
    }

    [Test]
    public async Task GetByFilters_ConFechas_NoLasConfundeConEstadoDisponible() {
        // Arrange
        await GuardarFlotaQuery();
        await using var db = CrearContexto();
        var repository = new VehiculoRepository(db, NullLogger<VehiculoRepository>.Instance);
        var request = new QueryRequestVehiculoDto {
            Marca = "Renault", FechaRecogida = new DateTime(2090, 1, 1), FechaDevolucion = new DateTime(2090, 1, 4)
        };

        // Act
        var resultado = await repository.GetByFilters(request);

        // Assert
        resultado.Should().ContainSingle().Which.Estado.Should().Be(EstadoVehiculo.Reservado);
        // Solo se verifica el reparto de responsabilidades, no disponibilidad real por fechas.
    }

    private static QueryRequestVehiculoDto CrearFiltro(string campo, string valor) => campo switch {
        "Marca" => new() { Marca = valor }, "Modelo" => new() { Modelo = valor },
        "Categoria" => new() { Categoria = valor }, "Estado" => new() { Estado = valor },
        "Combustible" => new() { Combustible = valor }, "Transmision" => new() { Transmision = valor },
        "PlazasMinimas" => new() { PlazasMinimas = int.Parse(valor) },
        "MaleteroLitrosMinimos" => new() { MaleteroLitrosMinimos = int.Parse(valor) },
        "AutonomiaKmMinima" => new() { AutonomiaKmMinima = int.Parse(valor) },
        _ => throw new ArgumentException("Campo de prueba no admitido", nameof(campo))
    };

    private async Task GuardarFlotaQuery() {
        var concesionario = await GuardarConcesionario();
        var corolla = CrearVehiculo(CrearFoto("corolla.jpg"), concesionario.Id);
        var megane = CrearVehiculo(CrearFoto("megane.jpg"), concesionario.Id) with {
            Matrícula = "2345CDF", Marca = "Renault", Modelo = "Megane", Acriss = "CDMD",
            Carroceria = TipoCarroceria.Hatchback, Combustible = TipoCombustible.Diesel,
            Transmision = TipoTransmision.Manual, MaleteroLitros = 400, AutonomiaKm = 600
        };
        megane.CambiarEstadoMapper(EstadoVehiculo.Reservado);
        var ariya = CrearVehiculo(CrearFoto("ariya.jpg"), concesionario.Id) with {
            Matrícula = "3456FGH", Marca = "Nissan", Modelo = "Ariya", Acriss = "IFAE",
            Carroceria = TipoCarroceria.Suv, Combustible = TipoCombustible.Electrico,
            Plazas = 7, MaleteroLitros = 500, AutonomiaKm = 450
        };
        ariya.CambiarEstadoMapper(EstadoVehiculo.Mantenimiento);
        var retirado = CrearVehiculo(CrearFoto("retirado.jpg"), concesionario.Id) with {
            Matrícula = "4567JKL", Activo = false, DeletedAt = Fecha
        };
        retirado.CambiarEstadoMapper(EstadoVehiculo.Retirado);
        await using var db = CrearContexto();
        db.Vehiculos.AddRange(retirado, ariya, megane, corolla);
        await db.SaveChangesAsync();
    }
}
