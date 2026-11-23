using Alquiler_Vehiculos.Dto.Catalogo_Vehiculos.Vehiculo;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Extensions;
using Alquiler_Vehiculos.Models.Vehiculo;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Services;

public partial class VehiculoServiceTests {
    [TestCase(false, "inexistente", "Marca")]
    [TestCase(true, "inexistente", "Marca")]
    [TestCase(false, "baja", "Marca")]
    [TestCase(true, "baja", "Marca")]
    [TestCase(false, "modeloAjeno", "Modelo")]
    [TestCase(true, "modeloAjeno", "Modelo")]
    [TestCase(false, "marcaMayusculas", "Marca")]
    [TestCase(true, "marcaMayusculas", "Marca")]
    [TestCase(false, "modeloMayusculas", "Modelo")]
    [TestCase(true, "modeloMayusculas", "Modelo")]
    public async Task Guardar_CombinacionFueraDelCatalogo_Devuelve400SinPersistir(bool actualizar, string caso, string campo) {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toyota = new MarcaModel(ObjectId.Empty, "Toyota", "Japón", "https://toyota.example",
            ["Corolla"], caso == "baja", fecha, null, caso == "baja" ? fecha : null);
        var renault = new MarcaModel(ObjectId.Empty, "Renault", "Francia", "https://renault.example",
            ["Megane"], false, fecha, null, null);
        _marcaRepository.Setup(r => r.GetAll()).ReturnsAsync([toyota, renault]);
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        var marca = caso == "inexistente" ? "Inventada" : caso == "marcaMayusculas" ? "TOYOTA" : "Toyota";
        var modelo = caso == "modeloAjeno" ? "Megane" : caso == "modeloMayusculas" ? "COROLLA" : "Corolla";
        var create = CrearCreateValido() with { Marca = marca, Modelo = modelo };
        var update = CrearUpdateValido() with { Marca = marca, Modelo = modelo };

        // Act
        var resultado = actualizar ? await _service.Update(update, VehiculoId) : await _service.Create(create);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>().Which.ValidationErrors.Should().ContainSingle()
            .Which.Key.Should().Be(campo);
        resultado.Error.ToHttpResult<VehiculoDto>().Result.Should().BeOfType<BadRequestObjectResult>();
        _marcaRepository.Verify(r => r.GetAll(), Times.Once);
        _concesionarioRepository.Verify(r => r.ExistById(1), Times.Once);
        _repository.Verify(r => r.GetById(VehiculoId), actualizar ? Times.Once() : Times.Never());
        _repository.Verify(r => r.ExistByMatricula(Matricula), actualizar ? Times.Never() : Times.Once());
        _repository.Verify(r => r.Create(It.IsAny<VehiculoModel>()), Times.Never);
        _transactionsService.Verify(t => t.UpdateAsync(It.IsAny<string>(), It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()), Times.Never);
        _fotoRepository.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<VehiculoDto>(), It.IsAny<TimeSpan?>(),
            It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false, "Toyota", "Corolla")]
    [TestCase(true, "Toyota", "Corolla")]
    [TestCase(false, "Renault", "Megane")]
    [TestCase(true, "Renault", "Megane")]
    public async Task Guardar_CombinacionValida_ConservaMarcaYModeloDelCatalogo(bool actualizar, string marca, string modelo) {
        // Arrange
        _repository.Setup(r => r.GetById(VehiculoId)).ReturnsAsync(CrearModelo());
        _fotoRepository.Setup(r => r.GetById(FotoId)).ReturnsAsync(CrearFoto());
        if (actualizar) {
            _transactionsService.Setup(t => t.UpdateAsync(VehiculoId, It.IsAny<VehiculoModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, VehiculoModel v, CancellationToken _) => Result.Success<VehiculoModel, DomainError>(v));
        } else {
            _repository.Setup(r => r.Create(It.IsAny<VehiculoModel>()))
                .ReturnsAsync((VehiculoModel v) => Result.Success<VehiculoModel, DomainError>(v with { Id = VehiculoId }));
        }
        var create = CrearCreateValido() with { Marca = marca, Modelo = modelo };
        var update = CrearUpdateValido() with { Marca = marca, Modelo = modelo };

        // Act
        var resultado = actualizar ? await _service.Update(update, VehiculoId) : await _service.Create(create);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Marca.Should().Be(marca);
        resultado.Value.Modelo.Should().Be(modelo);
        _marcaRepository.Verify(r => r.GetAll(), Times.Once);
        _concesionarioRepository.Verify(r => r.ExistById(1), Times.Once);
        _fotoRepository.Verify(r => r.GetById(FotoId), Times.Once);
        _repository.Verify(r => r.GetById(VehiculoId), actualizar ? Times.Once() : Times.Never());
        _repository.Verify(r => r.ExistByMatricula(Matricula), actualizar ? Times.Never() : Times.Once());
        _repository.Verify(r => r.Create(It.Is<VehiculoModel>(v => v.Marca == marca && v.Modelo == modelo)), actualizar ? Times.Never() : Times.Once());
        _transactionsService.Verify(t => t.UpdateAsync(VehiculoId,
            It.Is<VehiculoModel>(v => v.Marca == marca && v.Modelo == modelo), It.IsAny<CancellationToken>()), actualizar ? Times.Once() : Times.Never());
        _cache.Verify(c => c.RemoveAsync(ClaveVehiculo + VehiculoId, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(ClaveMatricula + Matricula, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(ClaveVehiculo + VehiculoId,
            It.Is<VehiculoDto>(v => v.Marca == marca && v.Modelo == modelo), It.IsAny<TimeSpan?>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(ClaveMatricula + Matricula,
            It.Is<VehiculoDto>(v => v.Marca == marca && v.Modelo == modelo), It.IsAny<TimeSpan?>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
