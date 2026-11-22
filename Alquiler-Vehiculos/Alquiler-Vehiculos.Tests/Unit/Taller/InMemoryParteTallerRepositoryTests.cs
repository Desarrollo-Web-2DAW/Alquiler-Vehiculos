using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Models.Taller;
using Alquiler_Vehiculos.Repositories.Taller;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Taller;

/// <summary>
/// Tests unitarios del almacén de partes de taller en memoria (InMemoryParteTallerRepository).
/// Requisitos cubiertos: RF-08 (un solo parte abierto por vehículo, de forma atómica)
/// y CU-08 (los abiertos salen ordenados por fecha de entrada).
/// </summary>
[Category("Unit")]
public class InMemoryParteTallerRepositoryTests {
    private InMemoryParteTallerRepository _repositorio = null!;

    [SetUp]
    public void Setup() {
        _repositorio = new InMemoryParteTallerRepository();
    }

    private static ParteTaller Parte(string vehiculoId = "VEHICULO001", EstadoParte estado = EstadoParte.Abierto,
        DateTime? fecha = null) {
        return new ParteTaller {
            VehiculoId = vehiculoId,
            Descripcion = "Revisión",
            QuienLoTramita = Guid.NewGuid(),
            Estado = estado,
            Fecha = fecha ?? DateTime.UtcNow
        };
    }

    #region Alta de partes

    /// <summary>
    /// Dado que el almacén está vacío, cuando se añade un parte,
    /// entonces se guarda y devuelve true.
    /// </summary>
    [Test]
    public async Task Agregar_ParteNueva_LaGuardayDevuelveTrue() {
        // Arrange
        var parte = Parte();

        // Act
        var añadido = await _repositorio.AgregarAsync(parte);

        // Assert
        añadido.Should().BeTrue();
        var guardada = await _repositorio.ObtenerPorIdAsync(parte.Id);
        guardada.Should().NotBeNull();
        guardada!.VehiculoId.Should().Be("VEHICULO001");
    }

    /// <summary>
    /// Dado que el vehículo ya tiene un parte abierto, cuando se añade otro,
    /// entonces devuelve false y no se duplica: la exclusión es atómica (RF-08).
    /// </summary>
    [Test]
    public async Task Agregar_SegundoParteAbiertoParaElMismoVehiculo_DevuelveFalseYNoLoDuplica() {
        // Arrange
        await _repositorio.AgregarAsync(Parte());

        // Act
        var añadido = await _repositorio.AgregarAsync(Parte());

        // Assert
        añadido.Should().BeFalse();
        var historial = await _repositorio.ObtenerPorVehiculoAsync("VEHICULO001");
        historial.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que el parte anterior del vehículo ya está cerrado, cuando se añade otro,
    /// entonces se acepta: cada entrada al taller abre un parte nuevo (RF-08).
    /// </summary>
    [Test]
    public async Task Agregar_ParteTrasCerrarLaAnterior_LaGuarda() {
        // Arrange
        var primero = Parte();
        await _repositorio.AgregarAsync(primero);
        primero.Estado = EstadoParte.Cerrado;
        await _repositorio.ActualizarAsync(primero);

        // Act
        var añadido = await _repositorio.AgregarAsync(Parte());

        // Assert
        añadido.Should().BeTrue();
        var historial = await _repositorio.ObtenerPorVehiculoAsync("VEHICULO001");
        historial.Should().HaveCount(2);
    }

    /// <summary>
    /// Dado que los partes son de vehículos distintos, cuando se añaden,
    /// entonces se guardan los dos sin interferir.
    /// </summary>
    [Test]
    public async Task Agregar_PartesAbiertasDeDistintosVehiculos_LasGuarda() {
        // Arrange & Act
        await _repositorio.AgregarAsync(Parte("VEHICULO001"));
        await _repositorio.AgregarAsync(Parte("VEHICULO002"));

        // Assert
        (await _repositorio.ObtenerAbiertosAsync()).Should().HaveCount(2);
    }

    #endregion

    #region Consultas

    /// <summary>
    /// Dado que hay un parte guardado, cuando se consulta,
    /// entonces devuelve una copia: los cambios sin guardar no se cuelan al almacén.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_ParteGuardada_DevuelveUnaCopiaIndependiente() {
        // Arrange
        var parte = Parte();
        await _repositorio.AgregarAsync(parte);

        // Act
        var consultada = await _repositorio.ObtenerPorIdAsync(parte.Id);
        consultada!.Estado = EstadoParte.Cerrado;

        // Assert
        var otraConsulta = await _repositorio.ObtenerPorIdAsync(parte.Id);
        otraConsulta!.Estado.Should().Be(EstadoParte.Abierto);
    }

    /// <summary>
    /// Dado que el parte no existe, cuando se consulta,
    /// entonces devuelve null.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_ParteNoExiste_DevuelveNull() {
        // Arrange & Act & Assert
        (await _repositorio.ObtenerPorIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    /// <summary>
    /// Dado que un vehículo tiene un parte abierto, cuando se busca,
    /// entonces lo devuelve.
    /// </summary>
    [Test]
    public async Task ObtenerAbiertoPorVehiculo_ConParteAbierta_LaDevuelve() {
        // Arrange
        await _repositorio.AgregarAsync(Parte());

        // Act
        var abierto = await _repositorio.ObtenerAbiertoPorVehiculoAsync("VEHICULO001");

        // Assert
        abierto.Should().NotBeNull();
        abierto!.Estado.Should().Be(EstadoParte.Abierto);
    }

    /// <summary>
    /// Dado que el vehículo solo tiene partes cerrados, cuando se busca el abierto,
    /// entonces devuelve null.
    /// </summary>
    [Test]
    public async Task ObtenerAbiertoPorVehiculo_SoloCerrados_DevuelveNull() {
        // Arrange
        var parte = Parte(estado: EstadoParte.Cerrado);
        await _repositorio.AgregarAsync(parte);

        // Act & Assert
        (await _repositorio.ObtenerAbiertoPorVehiculoAsync("VEHICULO001")).Should().BeNull();
    }

    /// <summary>
    /// Dado que hay partes abiertos y cerrados de distintas fechas, cuando se piden
    /// los abiertos, entonces salen solo los abiertos y ordenados por fecha de entrada (CU-08).
    /// </summary>
    [Test]
    public async Task ObtenerAbiertos_ConMezcla_DevuelveSoloAbiertosOrdenadosPorFecha() {
        // Arrange
        var hoy = DateTime.UtcNow;
        await _repositorio.AgregarAsync(Parte("VEHICULO002", fecha: hoy));
        await _repositorio.AgregarAsync(Parte("VEHICULO001", fecha: hoy.AddMinutes(-30)));
        await _repositorio.AgregarAsync(Parte("VEHICULO003", estado: EstadoParte.Cerrado, fecha: hoy.AddMinutes(-60)));

        // Act
        var abiertos = await _repositorio.ObtenerAbiertosAsync();

        // Assert
        abiertos.Select(p => p.VehiculoId).Should().Equal("VEHICULO001", "VEHICULO002");
    }

    /// <summary>
    /// Dado que un vehículo tiene varios partes, cuando se pide su historial,
    /// entonces devuelve todos ordenados por fecha de entrada.
    /// </summary>
    [Test]
    public async Task ObtenerPorVehiculo_ConHistorial_LoDevuelveOrdenadoPorFecha() {
        // Arrange
        var hoy = DateTime.UtcNow;
        var primero = Parte("VEHICULO001", EstadoParte.Cerrado, hoy.AddDays(-1));
        var segundo = Parte("VEHICULO001", fecha: hoy);
        await _repositorio.AgregarAsync(primero);
        await _repositorio.AgregarAsync(segundo);
        await _repositorio.AgregarAsync(Parte("VEHICULO002", fecha: hoy));

        // Act
        var historial = await _repositorio.ObtenerPorVehiculoAsync("VEHICULO001");

        // Assert
        historial.Should().HaveCount(2);
        historial.Select(p => p.Id).Should().Equal(primero.Id, segundo.Id);
    }

    #endregion

    #region Actualización

    /// <summary>
    /// Dado que un parte está guardado, cuando se actualiza,
    /// entonces se conservan los cambios (por ejemplo, el cierre).
    /// </summary>
    [Test]
    public async Task Actualizar_ParteExistente_GuardaLosCambios() {
        // Arrange
        var parte = Parte();
        await _repositorio.AgregarAsync(parte);
        var consultada = await _repositorio.ObtenerPorIdAsync(parte.Id);
        consultada!.Estado = EstadoParte.Cerrado;

        // Act
        await _repositorio.ActualizarAsync(consultada);

        // Assert
        var guardada = await _repositorio.ObtenerPorIdAsync(parte.Id);
        guardada!.Estado.Should().Be(EstadoParte.Cerrado);
    }

    #endregion

    /// <summary>
    /// Dado que la llamada llega con la cancelación solicitada, cuando se añade,
    /// entonces se cancela sin guardar (contrato del token).
    /// </summary>
    [Test]
    public async Task Agregar_ConTokenCancelado_LanzaOperacionCancelada() {
        // Arrange
        var cancelado = new CancellationToken(canceled: true);
        Func<Task> act = () => _repositorio.AgregarAsync(Parte(), cancelado);

        // Act & Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
