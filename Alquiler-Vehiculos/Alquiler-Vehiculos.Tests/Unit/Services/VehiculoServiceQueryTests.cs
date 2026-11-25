using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Query;
using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Models.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using Alquiler_Vehiculos.Tests.Unit.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

public partial class VehiculoServiceTests {
    [Test]
    public async Task GetByMatricula_EnCache_DevuelveDtoSinConsultarRepositorio() {
        // Arrange
        var dto = CrearDto();
        _cache.Setup(c => c.GetAsync<VehiculoDto>(ClaveMatricula + Matricula, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var resultado = await _service.GetByMatricula(Matricula);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeSameAs(dto);
        _cache.Verify(c => c.GetAsync<VehiculoDto>(ClaveMatricula + Matricula, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.GetAsync<VehiculoDto>(ClaveVehiculo + Matricula, It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.GetByMatricula(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetByMatricula_FueraDeCache_MapeaYGuardaConAmbasClaves(bool idIgualAMatricula) {
        // Arrange
        var modelo = CrearModelo() with { Id = idIgualAMatricula ? Matricula : VehiculoId };
        var esperado = CrearDto() with { Id = modelo.Id };
        _repository.Setup(r => r.GetByMatricula(Matricula)).ReturnsAsync(modelo);

        // Act
        var resultado = await _service.GetByMatricula(Matricula);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEquivalentTo(esperado);
        _repository.Verify(r => r.GetByMatricula(Matricula), Times.Once);
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _cache.Verify(c => c.GetAsync<VehiculoDto>(ClaveMatricula + Matricula, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.GetAsync<VehiculoDto>(ClaveVehiculo + Matricula, It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.SetAsync(ClaveVehiculo + modelo.Id,
            It.Is<VehiculoDto>(d => d.Id == modelo.Id && d.Matricula == Matricula), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(ClaveMatricula + Matricula,
            It.Is<VehiculoDto>(d => d.Id == modelo.Id && d.Matricula == Matricula), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetByMatricula_NoExiste_Devuelve404SinCachear() {
        // Arrange
        _repository.Setup(r => r.GetByMatricula(Matricula)).ReturnsAsync((VehiculoModel?)null);

        // Act
        var resultado = await _service.GetByMatricula(Matricula);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(VehiculoError.NotFoundVehiculoMatricula(Matricula));
        resultado.Error.ToHttpResult().Should().BeOfType<NotFoundObjectResult>();
        _repository.Verify(r => r.GetByMatricula(Matricula), Times.Once);
        _cache.Verify(c => c.GetAsync<VehiculoDto>(ClaveMatricula + Matricula, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCaseSource(typeof(QueryVehiculoCasos), nameof(QueryVehiculoCasos.Invalidos))]
    public async Task GetByFilters_EntradaInvalida_Devuelve400SinConsultar(QueryRequestVehiculoDto request, string campo) {
        // Arrange
        const bool esAdmin = true;

        // Act
        var resultado = await _service.GetByFilters(request, esAdmin);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey(campo);
        resultado.Error.ToHttpResult().Should().BeOfType<BadRequestObjectResult>();
        _repository.Verify(r => r.GetByFilters(It.IsAny<QueryRequestVehiculoDto>()), Times.Never);
        VerificarConsultaSinEfectosSecundarios();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetByFilters_SinCoincidencias_DevuelveExitoVacio(bool esAdmin) {
        // Arrange
        var request = new QueryRequestVehiculoDto { Marca = "No existe" };
        _repository.Setup(r => r.GetByFilters(request)).ReturnsAsync([]);

        // Act
        var resultado = await _service.GetByFilters(request, esAdmin);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
        _repository.Verify(r => r.GetByFilters(request), Times.Once);
        VerificarConsultaSinEfectosSecundarios();
    }

    [TestCase(true, 2)]
    [TestCase(false, 1)]
    public async Task GetByFilters_SegunPerfil_ConservaTotalesYOcultaSoloGruposSinLibres(bool esAdmin, int gruposEsperados) {
        // Arrange
        var request = new QueryRequestVehiculoDto { Marca = "Toyota" };
        var vehiculos = new List<VehiculoModel> { CrearModelo() };
        foreach (var estado in new[] { EstadoVehiculo.Reservado, EstadoVehiculo.EnAlquiler,
                     EstadoVehiculo.Mantenimiento, EstadoVehiculo.Retirado }) {
            var unidad = CrearModelo() with { Id = estado.ToString(), Activo = estado != EstadoVehiculo.Retirado };
            unidad.CambiarEstadoMapper(estado);
            vehiculos.Add(unidad);
        }
        // Incluso si un dato incoherente está Disponible, Activo=false impide contarlo como libre.
        vehiculos.Add(CrearModelo() with { Id = "inactivo", Activo = false });
        var sinLibres = CrearModelo() with { Id = "otro-modelo", Modelo = "Yaris" };
        sinLibres.CambiarEstadoMapper(EstadoVehiculo.Reservado);
        vehiculos.Add(sinLibres);
        _repository.Setup(r => r.GetByFilters(request)).ReturnsAsync(vehiculos);

        // Act
        var resultado = await _service.GetByFilters(request, esAdmin);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        var grupos = resultado.Value.ToList();
        grupos.Should().HaveCount(gruposEsperados);
        grupos.Single(g => g.Modelo == "Corolla").Should().BeEquivalentTo(new QueryResponseVehiculoDto {
            Marca = "Toyota", Modelo = "Corolla", Categoria = "Hatchback", UnidadesTotales = 6, UnidadesLibres = 1
        });
        if (esAdmin) grupos.Single(g => g.Modelo == "Yaris").Should().BeEquivalentTo(new { UnidadesTotales = 1, UnidadesLibres = 0 });
        else grupos.Should().OnlyContain(g => g.UnidadesLibres > 0);
        _repository.Verify(r => r.GetByFilters(request), Times.Once);
        VerificarConsultaSinEfectosSecundarios();
    }

    [Test]
    public async Task GetByFilters_SinIndicarPerfil_AplicaVistaPublica() {
        // Arrange
        var request = new QueryRequestVehiculoDto();
        var ocupado = CrearModelo();
        ocupado.CambiarEstadoMapper(EstadoVehiculo.Mantenimiento);
        _repository.Setup(r => r.GetByFilters(request)).ReturnsAsync([ocupado]);

        // Act
        var resultado = await _service.GetByFilters(request);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEmpty();
        _repository.Verify(r => r.GetByFilters(request), Times.Once);
        VerificarConsultaSinEfectosSecundarios();
    }

    [Test]
    public async Task GetByFilters_MarcasModelosYCategoriasDistintos_NoMezclaGrupos() {
        // Arrange
        var request = new QueryRequestVehiculoDto();
        _repository.Setup(r => r.GetByFilters(request)).ReturnsAsync([
            CrearModelo(), CrearModelo() with { Id = "2", Marca = "Otra" },
            CrearModelo() with { Id = "3", Modelo = "Otro" },
            CrearModelo() with { Id = "4", Carroceria = TipoCarroceria.Sedan }
        ]);

        // Act
        var resultado = await _service.GetByFilters(request, true);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().HaveCount(4).And.OnlyContain(g => g.UnidadesTotales == 1 && g.UnidadesLibres == 1);
        resultado.Value.Select(g => (g.Marca, g.Modelo, g.Categoria)).Should().BeEquivalentTo(new[] {
            ("Toyota", "Corolla", "Hatchback"), ("Otra", "Corolla", "Hatchback"),
            ("Toyota", "Otro", "Hatchback"), ("Toyota", "Corolla", "Sedan")
        });
        _repository.Verify(r => r.GetByFilters(request), Times.Once);
        VerificarConsultaSinEfectosSecundarios();
    }

    [Test]
    public async Task GetByFilters_FechasValidasSinIntegracion_NoInventaDisponibilidad() {
        // Arrange
        var request = new QueryRequestVehiculoDto {
            FechaRecogida = new DateTime(2090, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            FechaDevolucion = new DateTime(2090, 1, 4, 10, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var resultado = await _service.GetByFilters(request);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<InternalError>().Which.Message.Should().Contain("aún no está integrada");
        resultado.Error.ToHttpResult().Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _repository.Verify(r => r.GetByFilters(It.IsAny<QueryRequestVehiculoDto>()), Times.Never);
        VerificarConsultaSinEfectosSecundarios();
    }

    [Test]
    public async Task Update_ColorInvalido_DevuelveErrorDeCampoSinConsultar() {
        // Arrange
        var request = CrearUpdateValido() with { Color = "NoExiste" };

        // Act
        var resultado = await _service.Update(request, VehiculoId);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainKey("Color");
        _repository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        VerificarConsultaSinEfectosSecundarios();
    }

    private void VerificarConsultaSinEfectosSecundarios() {
        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<VehiculoModel>()), Times.Never);
        _repository.Verify(r => r.SoftDelete(It.IsAny<string>()), Times.Never);
        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _concesionarioRepository.Verify(r => r.ExistById(It.IsAny<long>()), Times.Never);
        _transactionsService.Verify(t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactionsService.Verify(t => t.RestoreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
