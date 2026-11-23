using System.Reflection;
using Alquiler_Vehiculos.Controllers.Vehiculo;
using Alquiler_Vehiculos.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Alquiler_Vehiculos.Tests.Unit.Controllers;

/// <summary>Comprueba los atributos del controller; no sustituye las pruebas HTTP de 401/403.</summary>
[TestFixture]
[Category("Unit")]
public class VehiculoControllerPermisosTests {
    [Test]
    public void Controller_ProteccionGeneral_ExigeIdentificacionSinImponerAdmin() {
        // Arrange
        var tipo = typeof(VehiculoController);

        // Act
        var atributos = tipo.GetCustomAttributes<AuthorizeAttribute>().ToArray();

        // Assert
        var atributo = atributos.Should().ContainSingle().Which;
        atributo.Policy.Should().BeNullOrEmpty();
        atributo.Roles.Should().BeNullOrEmpty();
    }

    [TestCase(nameof(VehiculoController.GetAll), PolicyNames.Trabajador)]
    [TestCase(nameof(VehiculoController.GetPaged), PolicyNames.Trabajador)]
    [TestCase(nameof(VehiculoController.Update), PolicyNames.Trabajador)]
    [TestCase(nameof(VehiculoController.Create), PolicyNames.Admin)]
    [TestCase(nameof(VehiculoController.Delete), PolicyNames.Admin)]
    [TestCase(nameof(VehiculoController.Restore), PolicyNames.Admin)]
    public void AccionInterna_PermisoDeLaMatriz_AplicaPoliticaSinAccesoAnonimo(string accion, string politica) {
        // Arrange
        var metodo = typeof(VehiculoController).GetMethod(accion)!;

        // Act
        var autorizacion = metodo.GetCustomAttributes<AuthorizeAttribute>().ToArray();
        var anonimo = metodo.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        autorizacion.Should().ContainSingle().Which.Policy.Should().Be(politica);
        anonimo.Should().BeNull();
    }

    [TestCase(nameof(VehiculoController.GetById))]
    [TestCase(nameof(VehiculoController.GetByMatricula))]
    [TestCase(nameof(VehiculoController.GetByFilters))]
    public void ConsultaPublica_Catalogo_PermiteAccesoAnonimo(string accion) {
        // Arrange
        var metodo = typeof(VehiculoController).GetMethod(accion)!;

        // Act
        var anonimo = metodo.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        anonimo.Should().NotBeNull();
    }
}
