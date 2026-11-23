using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Realtime;
using Alquiler_Vehiculos.Repositories.Taller;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Validators.Taller;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Taller;

/// <summary>
/// Tests unitarios del servicio de partes de taller (ParteTallerService).
/// Requisitos cubiertos: RF-08 (partes de taller y su máquina de estados),
/// RF-12 (solo el tipo Mecánico abre y cierra partes), RF-01 (comprobación de
/// partes abiertos antes de dar de baja un vehículo) y CU-08 (vehículos en
/// taller con su fecha de entrada).
/// </summary>
[Category("Unit")]
public class ParteTallerServiceTests {
    private InMemoryParteTallerRepository _repositorio = null!;
    private ParteTallerService _servicio = null!;
    private Mock<IHubContext<NotificacionesHub>> _hubContext = null!;
    private Mock<IHubClients> _hubClients = null!;
    private Mock<IClientProxy> _clientProxy = null!;


    [SetUp]
    public void Setup() {
        _repositorio = new InMemoryParteTallerRepository();

        _clientProxy = new Mock<IClientProxy>();
        _hubClients = new Mock<IHubClients>();
        _hubContext = new Mock<IHubContext<NotificacionesHub>>();

        _hubClients
            .Setup(c => c.Group(NotificacionesHub.GrupoTrabajadoresOficina))
            .Returns(_clientProxy.Object);

        _hubContext
            .Setup(h => h.Clients)
            .Returns(_hubClients.Object);

        _servicio = new ParteTallerService(
            _repositorio,
            new AbrirParteValidator(),
            NullLogger<ParteTallerService>.Instance,
            _hubContext.Object);
    }

    private static Trabajador Mecanico() {
        return new Trabajador { Nombre = "Marta", Apellidos = "Méndez", Tipo = TipoTrabajador.Mecanico };
    }

    private static AbrirParteDto DatosValidos(string vehiculoId = "VEHICULO001") {
        return new AbrirParteDto { VehiculoId = vehiculoId, Descripcion = "Cambio de frenos" };
    }

    #region Apertura de partes (RF-08, CU-07)

    /// <summary>
    /// Dado que un mecánico activo abre un parte con datos válidos, cuando se abre,
    /// entonces queda en estado Abierto con sus datos y el trabajador que lo tramita.
    /// </summary>
    [Test]
    public async Task AbrirParte_ParteValidoYMecanico_AbreElParteAbierto() {
        // Arrange
        var tramitador = Mecanico();
        var dto = DatosValidos();

        // Act
        var resultado = await _servicio.AbrirParteAsync(tramitador, dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        var parte = resultado.Value;
        parte.Id.Should().NotBeEmpty();
        parte.Estado.Should().Be(EstadoParte.Abierto);
        parte.VehiculoId.Should().Be("VEHICULO001");
        parte.Descripcion.Should().Be("Cambio de frenos");
        parte.QuienLoTramita.Should().Be(tramitador.Id);
        parte.Fecha.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Dado que los datos de apertura no cumplen el formato, cuando se abre,
    /// entonces devuelve un error de validación con el detalle por campo (RF-01, RF-08).
    /// </summary>
    [Test]
    public async Task AbrirParte_DatosInvalidos_DevuelveErrorDeValidacionConCampos() {
        // Arrange
        var dto = new AbrirParteDto { VehiculoId = "corto", Descripcion = "" };

        // Act
        var resultado = await _servicio.AbrirParteAsync(Mecanico(), dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("VehiculoId");
        error.ValidationErrors.Should().ContainKey("Descripcion");
    }

    /// <summary>
    /// Dado que el tramitador no es de tipo Mecánico, cuando se abre un parte,
    /// entonces se rechaza con 403 y no se guarda nada (RF-08, RF-12).
    /// </summary>
    [Test]
    public async Task AbrirParte_TrabajadorQueNoEsMecanico_DevuelveForbiddenYNoGuardaNada() {
        // Arrange
        var tramitador = new Trabajador { Tipo = TipoTrabajador.Operaciones };

        // Act
        var resultado = await _servicio.AbrirParteAsync(tramitador, DatosValidos());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ForbiddenError>().Subject;
        error.Code.Should().Be("SOLO_MECANICO");
        (await _repositorio.ObtenerAbiertosAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Dado que el mecánico está inactivo, cuando se abre un parte,
    /// entonces se rechaza con el error de negocio del trabajador (RF-12).
    /// </summary>
    [Test]
    public async Task AbrirParte_TrabajadorInactivo_DevuelveErrorDeNegocio() {
        // Arrange
        var tramitador = new Trabajador { Tipo = TipoTrabajador.Mecanico, Activo = false };

        // Act
        var resultado = await _servicio.AbrirParteAsync(tramitador, DatosValidos());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>()
            .Which.Code.Should().Be("TRABAJADOR_INACTIVO");
    }

    /// <summary>
    /// Dado que el mecánico está dado de baja lógica, cuando se abre un parte,
    /// entonces también se rechaza (RF-12).
    /// </summary>
    [Test]
    public async Task AbrirParte_TrabajadorDadoDeBaja_DevuelveErrorDeNegocio() {
        // Arrange
        var tramitador = new Trabajador { Tipo = TipoTrabajador.Mecanico, IsDeleted = true };

        // Act
        var resultado = await _servicio.AbrirParteAsync(tramitador, DatosValidos());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>()
            .Which.Code.Should().Be("TRABAJADOR_INACTIVO");
    }

    /// <summary>
    /// Dado que el vehículo ya tiene un parte abierto, cuando se intenta abrir otro,
    /// entonces se rechaza con 409: el vehículo no puede entrar dos veces al taller (RF-08).
    /// </summary>
    [Test]
    public async Task AbrirParte_VehiculoYaTieneParteAbierto_DevuelveConflicto() {
        // Arrange
        await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Act
        var resultado = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ConflictError>().Subject;
        error.Code.Should().Be("PARTE_ABIERTA_EXISTENTE");
    }

    /// <summary>
    /// Dado que el parte anterior ya está cerrado, cuando se abre otro para el mismo
    /// vehículo, entonces se permite: cada entrada al taller abre un parte nuevo (RF-08).
    /// </summary>
    [Test]
    public async Task AbrirParte_ParteAnteriorCerrado_AbreOtroParte() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());
        await _servicio.CerrarParteAsync(abierto.Value.Id, Mecanico());

        // Act
        var resultado = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Estado.Should().Be(EstadoParte.Abierto);
    }

    #endregion

    #region Cierre de partes (RF-08, CU-07)

    /// <summary>
    /// Dado que hay un parte abierto, cuando lo cierra un mecánico activo,
    /// entonces queda Cerrado y persistido (el vehículo ya puede volver a Disponible).
    /// </summary>
    [Test]
    public async Task CerrarParte_ParteAbierto_LoCerrayQuedaCerrado() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Act
        var resultado = await _servicio.CerrarParteAsync(abierto.Value.Id, Mecanico());

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Estado.Should().Be(EstadoParte.Cerrado);
        var guardado = await _repositorio.ObtenerPorIdAsync(abierto.Value.Id);
        guardado!.Estado.Should().Be(EstadoParte.Cerrado);
    }

    /// <summary>
    /// Dado que el parte no existe, cuando se intenta cerrar,
    /// entonces devuelve 404 (RF-08).
    /// </summary>
    [Test]
    public async Task CerrarParte_ParteNoExiste_DevuelveNotFound() {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var resultado = await _servicio.CerrarParteAsync(idInexistente, Mecanico());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFoundError>()
            .Which.Code.Should().Be("PARTE_NO_ENCONTRADA");
    }

    /// <summary>
    /// Dado que el parte ya está cerrado, cuando se intenta cerrar de nuevo,
    /// entonces se rechaza con 409: la transición no se puede repetir ni reabrir (RF-08, RF-32).
    /// </summary>
    [Test]
    public async Task CerrarParte_ParteYaCerrado_DevuelveConflicto() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());
        await _servicio.CerrarParteAsync(abierto.Value.Id, Mecanico());

        // Act
        var resultado = await _servicio.CerrarParteAsync(abierto.Value.Id, Mecanico());

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ConflictError>().Subject;
        error.Code.Should().Be("PARTE_YA_CERRADA");
    }

    /// <summary>
    /// Dado que quien cierra no es mecánico, cuando se intenta cerrar,
    /// entonces se rechaza con 403 y el parte sigue abierto (RF-08, RF-12).
    /// </summary>
    [Test]
    public async Task CerrarParte_TrabajadorQueNoEsMecanico_DevuelveForbiddenYElParteSigueAbierto() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Act
        var resultado = await _servicio.CerrarParteAsync(abierto.Value.Id,
            new Trabajador { Tipo = TipoTrabajador.Operaciones });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ForbiddenError>();
        var guardado = await _repositorio.ObtenerPorIdAsync(abierto.Value.Id);
        guardado!.Estado.Should().Be(EstadoParte.Abierto);
    }

    /// <summary>
    /// Dado que el mecánico está inactivo, cuando intenta cerrar un parte,
    /// entonces se rechaza con el error de negocio (RF-12).
    /// </summary>
    [Test]
    public async Task CerrarParte_TrabajadorInactivo_DevuelveErrorDeNegocio() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Act
        var resultado = await _servicio.CerrarParteAsync(abierto.Value.Id,
            new Trabajador { Tipo = TipoTrabajador.Mecanico, Activo = false });

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>()
            .Which.Code.Should().Be("TRABAJADOR_INACTIVO");
    }

    #endregion

    #region Consultas (RF-01, CU-08)

    /// <summary>
    /// Dado que existe un parte, cuando se consulta por su identificador,
    /// entonces devuelve sus datos.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_ParteExiste_DevuelveSusDatos() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos());

        // Act
        var resultado = await _servicio.ObtenerPorIdAsync(abierto.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(abierto.Value.Id);
        resultado.Value.VehiculoId.Should().Be("VEHICULO001");
    }

    /// <summary>
    /// Dado que el parte no existe, cuando se consulta,
    /// entonces devuelve 404.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_ParteNoExiste_DevuelveNotFound() {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var resultado = await _servicio.ObtenerPorIdAsync(idInexistente);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<NotFoundError>();
    }

    /// <summary>
    /// Dado que hay partes abiertos y cerrados, cuando se piden los abiertos,
    /// entonces devuelve solo los abiertos por fecha de entrada: los vehículos que
    /// están en taller ahora mismo (CU-08).
    /// </summary>
    [Test]
    public async Task ListarAbiertos_ConAbiertosYCerrados_DevuelveSoloAbiertosPorFecha() {
        // Arrange
        var primero = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO001"));
        var segundo = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO002"));
        await _servicio.CerrarParteAsync(primero.Value.Id, Mecanico());

        // Act
        var resultado = await _servicio.ListarAbiertosAsync();

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(1);
        resultado.Value.Single().Id.Should().Be(segundo.Value.Id);
        resultado.Value.Single().Fecha.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
    }

    /// <summary>
    /// Dado que un vehículo tiene varios partes en su historial, cuando se consulta,
    /// entonces devuelve todos sus partes por fecha de entrada.
    /// </summary>
    [Test]
    public async Task ListarPorVehiculo_ConHistorial_DevuelveTodosSusPartes() {
        // Arrange
        var primero = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO001"));
        await _servicio.CerrarParteAsync(primero.Value.Id, Mecanico());
        await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO001"));
        await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO002"));

        // Act
        var resultado = await _servicio.ListarPorVehiculoAsync("VEHICULO001");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(2);
        resultado.Value.Should().OnlyContain(p => p.VehiculoId == "VEHICULO001");
    }

    /// <summary>
    /// Dado que la consulta no trae vehículo, cuando se pide su historial,
    /// entonces devuelve un error de validación por campo.
    /// </summary>
    [Test]
    public async Task ListarPorVehiculo_SinVehiculo_DevuelveErrorDeValidacion() {
        // Arrange
        var sinVehiculo = "   ";

        // Act
        var resultado = await _servicio.ListarPorVehiculoAsync(sinVehiculo);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var error = resultado.Error.Should().BeOfType<ValidationError>().Subject;
        error.ValidationErrors.Should().ContainKey("vehiculoId");
    }

    /// <summary>
    /// Dado que un vehículo tiene un parte abierto, cuando se pregunta si tiene
    /// partes abiertos, entonces devuelve true (la comprobación de RF-01).
    /// </summary>
    [Test]
    public async Task TienePartesAbiertos_ConParteAbierto_DevuelveTrue() {
        // Arrange
        await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO001"));

        // Act
        var resultado = await _servicio.TienePartesAbiertosAsync("VEHICULO001");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }

    /// <summary>
    /// Dado que un vehículo solo tiene partes cerrados, cuando se pregunta,
    /// entonces devuelve false: se puede dar de baja (RF-01).
    /// </summary>
    [Test]
    public async Task TienePartesAbiertos_SoloConPartesCerrados_DevuelveFalse() {
        // Arrange
        var abierto = await _servicio.AbrirParteAsync(Mecanico(), DatosValidos("VEHICULO001"));
        await _servicio.CerrarParteAsync(abierto.Value.Id, Mecanico());

        // Act
        var resultado = await _servicio.TienePartesAbiertosAsync("VEHICULO001");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeFalse();
    }

    /// <summary>
    /// Dado que la consulta no trae vehículo, cuando se pregunta si tiene partes
    /// abiertos, entonces devuelve un error de validación por campo.
    /// </summary>
    [Test]
    public async Task TienePartesAbiertos_SinVehiculo_DevuelveErrorDeValidacion() {
        // Arrange
        var sinVehiculo = "";

        // Act
        var resultado = await _servicio.TienePartesAbiertosAsync(sinVehiculo);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
    }

    #endregion
}
