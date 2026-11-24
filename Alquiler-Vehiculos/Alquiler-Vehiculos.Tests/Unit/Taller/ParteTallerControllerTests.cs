using System.Security.Claims;
using Alquiler_Vehiculos.Controllers.Taller;
using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Enums.Taller;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Taller;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using Alquiler_Vehiculos.Services.Taller;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Taller;

/// <summary>
/// Tests unitarios de <see cref="ParteTallerController"/> doblando
/// <see cref="IParteTallerService"/> e <see cref="ITrabajadorRepository"/> con Moq.
/// Requisitos cubiertos: RF-08 (mapeo HTTP de las operaciones del parte) y RF-46
/// (el tramitador se resuelve desde el claim del token).
/// </summary>
[TestFixture]
[Category("Unit")]
public class ParteTallerControllerTests {
    private Mock<IParteTallerService> _servicio = null!;
    private Mock<ITrabajadorRepository> _trabajadores = null!;
    private ParteTallerController _controller = null!;

    [SetUp]
    public void SetUp() {
        _servicio = new Mock<IParteTallerService>();
        _trabajadores = new Mock<ITrabajadorRepository>();
        _controller = new ParteTallerController(
            _servicio.Object,
            _trabajadores.Object,
            NullLogger<ParteTallerController>.Instance);
    }

    private void ConClaimTrabajador(Guid trabajadorId) {
        var identidad = new ClaimsIdentity(
            [new Claim("trabajador_id", trabajadorId.ToString())], "TestAuth");
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
        };
    }

    private static Trabajador Mecanico(Guid id) {
        return new Trabajador { Id = id, Nombre = "Marta", Tipo = TipoTrabajador.Mecanico };
    }

    private static ParteTallerDto ParteDto() {
        return new ParteTallerDto(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(),
            "VEHICULO001", "Cambio de frenos", EstadoParte.Abierto);
    }

    private static AbrirParteDto Apertura() {
        return new AbrirParteDto { VehiculoId = "VEHICULO001", Descripcion = "Cambio de frenos" };
    }

    #region Apertura

    /// <summary>
    /// Dado que el token identifica a un mecánico, cuando abre un parte,
    /// entonces responde 201 con el parte creado.
    /// </summary>
    [Test]
    public async Task Abrir_ConClaimValido_Devuelve201ConElParte() {
        // Arrange
        var trabajadorId = Guid.NewGuid();
        ConClaimTrabajador(trabajadorId);
        _trabajadores.Setup(r => r.GetById(trabajadorId)).ReturnsAsync(Mecanico(trabajadorId));
        var dto = ParteDto();
        _servicio
            .Setup(s => s.AbrirParteAsync(It.IsAny<Trabajador>(), It.IsAny<AbrirParteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ParteTallerDto, DomainError>(dto));

        // Act
        var result = await _controller.Abrir(Apertura(), CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().Be(dto);
    }

    /// <summary>
    /// Dado que el token no tiene claim de trabajador, cuando se abre un parte,
    /// entonces responde 403.
    /// </summary>
    [Test]
    public async Task Abrir_SinClaimDeTrabajador_Devuelve403() {
        // Arrange
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.Abrir(Apertura(), CancellationToken.None);

        // Assert
        var forbidden = result.Result.Should().BeOfType<ObjectResult>().Subject;
        forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Dado que el trabajador del token ya no existe, cuando se abre un parte,
    /// entonces responde 404.
    /// </summary>
    [Test]
    public async Task Abrir_TrabajadorNoEncontrado_Devuelve404() {
        // Arrange
        var trabajadorId = Guid.NewGuid();
        ConClaimTrabajador(trabajadorId);
        _trabajadores.Setup(r => r.GetById(trabajadorId)).ReturnsAsync((Trabajador?)null);

        // Act
        var result = await _controller.Abrir(Apertura(), CancellationToken.None);

        // Assert
        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Dado que el servicio rechaza la apertura con un error de dominio, cuando se abre,
    /// entonces el error se traduce a su código HTTP (409 del parte abierto).
    /// </summary>
    [Test]
    public async Task Abrir_ErrorDeNegocio_DevuelveElCodigoDelError() {
        // Arrange
        var trabajadorId = Guid.NewGuid();
        ConClaimTrabajador(trabajadorId);
        _trabajadores.Setup(r => r.GetById(trabajadorId)).ReturnsAsync(Mecanico(trabajadorId));
        _servicio
            .Setup(s => s.AbrirParteAsync(It.IsAny<Trabajador>(), It.IsAny<AbrirParteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ParteTallerDto, DomainError>(
                ParteTallerError.ParteAbiertaExistente("VEHICULO001")));

        // Act
        var result = await _controller.Abrir(Apertura(), CancellationToken.None);

        // Assert
        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region Cierre

    /// <summary>
    /// Dado que el mecánico cierra un parte abierto, cuando se cierra,
    /// entonces responde 200 con el parte cerrado.
    /// </summary>
    [Test]
    public async Task Cerrar_OK_Devuelve200ConElParte() {
        // Arrange
        var trabajadorId = Guid.NewGuid();
        ConClaimTrabajador(trabajadorId);
        _trabajadores.Setup(r => r.GetById(trabajadorId)).ReturnsAsync(Mecanico(trabajadorId));
        var dto = ParteDto() with { Estado = EstadoParte.Cerrado };
        _servicio
            .Setup(s => s.CerrarParteAsync(It.IsAny<Guid>(), It.IsAny<Trabajador>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ParteTallerDto, DomainError>(dto));

        // Act
        var result = await _controller.Cerrar(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().Be(dto);
    }

    /// <summary>
    /// Dado que el servicio rechaza el cierre (parte ya cerrado), cuando se cierra,
    /// entonces responde 409.
    /// </summary>
    [Test]
    public async Task Cerrar_ParteYaCerrado_Devuelve409() {
        // Arrange
        var trabajadorId = Guid.NewGuid();
        ConClaimTrabajador(trabajadorId);
        _trabajadores.Setup(r => r.GetById(trabajadorId)).ReturnsAsync(Mecanico(trabajadorId));
        _servicio
            .Setup(s => s.CerrarParteAsync(It.IsAny<Guid>(), It.IsAny<Trabajador>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ParteTallerDto, DomainError>(
                ParteTallerError.YaCerrada(Guid.NewGuid())));

        // Act
        var result = await _controller.Cerrar(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Dado que el token no tiene claim de trabajador, cuando se cierra un parte,
    /// entonces responde 403.
    /// </summary>
    [Test]
    public async Task Cerrar_SinClaimDeTrabajador_Devuelve403() {
        // Arrange
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.Cerrar(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    #endregion

    #region Consultas

    /// <summary>
    /// Dado que el parte existe, cuando se consulta,
    /// entonces responde 200 con sus datos.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_Existente_Devuelve200ConElParte() {
        // Arrange
        var dto = ParteDto();
        _servicio
            .Setup(s => s.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ParteTallerDto, DomainError>(dto));

        // Act
        var result = await _controller.ObtenerPorId(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    /// <summary>
    /// Dado que el parte no existe, cuando se consulta,
    /// entonces responde 404.
    /// </summary>
    [Test]
    public async Task ObtenerPorId_NoExistente_Devuelve404() {
        // Arrange
        _servicio
            .Setup(s => s.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ParteTallerDto, DomainError>(ParteTallerError.NotFound(Guid.NewGuid())));

        // Act
        var result = await _controller.ObtenerPorId(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Dado que hay partes abiertos, cuando se listan (CU-08),
    /// entonces responde 200 con la lista.
    /// </summary>
    [Test]
    public async Task ListarAbiertos_ConPartes_Devuelve200ConLista() {
        // Arrange
        IReadOnlyList<ParteTallerDto> abiertos = [ParteDto()];
        _servicio
            .Setup(s => s.ListarAbiertosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ParteTallerDto>, DomainError>(abiertos));

        // Act
        var result = await _controller.ListarAbiertos(CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(abiertos);
    }

    /// <summary>
    /// Dado que la consulta de abiertos falla, cuando se lista,
    /// entonces el error de dominio se traduce a HTTP.
    /// </summary>
    [Test]
    public async Task ListarAbiertos_ConError_DevuelveElCodigoDelError() {
        // Arrange
        _servicio
            .Setup(s => s.ListarAbiertosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ParteTallerDto>, DomainError>(
                ValidationError.Create("fallo")));

        // Act
        var result = await _controller.ListarAbiertos(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Dado que un vehículo tiene historial, cuando se consulta,
    /// entonces responde 200 con sus partes.
    /// </summary>
    [Test]
    public async Task ListarPorVehiculo_ConHistorial_Devuelve200ConLista() {
        // Arrange
        IReadOnlyList<ParteTallerDto> historial = [ParteDto(), ParteDto()];
        _servicio
            .Setup(s => s.ListarPorVehiculoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ParteTallerDto>, DomainError>(historial));

        // Act
        var result = await _controller.ListarPorVehiculo("VEHICULO001", CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(historial);
    }

    /// <summary>
    /// Dado que la consulta del historial falla, cuando se pide,
    /// entonces el error de dominio se traduce a HTTP.
    /// </summary>
    [Test]
    public async Task ListarPorVehiculo_ConError_DevuelveElCodigoDelError() {
        // Arrange
        _servicio
            .Setup(s => s.ListarPorVehiculoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ParteTallerDto>, DomainError>(
                ValidationError.Create("vehículo obligatorio")));

        // Act
        var result = await _controller.ListarPorVehiculo("", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Dado que el vehículo tiene un parte abierto, cuando se comprueba (RF-01),
    /// entonces responde 200 con true.
    /// </summary>
    [Test]
    public async Task TienePartesAbiertos_ConParteAbierto_Devuelve200ConTrue() {
        // Arrange
        _servicio
            .Setup(s => s.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<bool, DomainError>(true));

        // Act
        var result = await _controller.TienePartesAbiertos("VEHICULO001", CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(new TienePartesAbiertosDto(true));
    }

    /// <summary>
    /// Dado que la comprobación de RF-01 falla, cuando se consulta,
    /// entonces el error de dominio se traduce a HTTP.
    /// </summary>
    [Test]
    public async Task TienePartesAbiertos_ConError_DevuelveElCodigoDelError() {
        // Arrange
        _servicio
            .Setup(s => s.TienePartesAbiertosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool, DomainError>(ValidationError.Create("vehículo obligatorio")));

        // Act
        var result = await _controller.TienePartesAbiertos("", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
