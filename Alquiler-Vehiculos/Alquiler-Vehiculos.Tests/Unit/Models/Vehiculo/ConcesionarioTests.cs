using Alquiler_Vehiculos.Models.Vehiculo;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios del record <see cref="Concesionario"/>.
    /// Verifica la construcción, la igualdad estructural y la auditoría
/// y el resto del contrato generado por el compilador (<c>with</c>, deconstrucción, <c>ToString</c>).
/// </summary>
public class ConcesionarioTests
{
    private static readonly DateTime Fecha = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static Concesionario CrearConcesionario() =>
        new(10, "Motor Norte", "Santander", "+34 942 000 111", "contacto@motornorte.es", false, Fecha, null, null);
    

    [Test]
    public void Constructor_ConTodosLosValores_AsignaCadaPropiedad()
    {
        // Arrange & Act
        var concesionario = CrearConcesionario();

        // Assert
        concesionario.Should().BeEquivalentTo(new
        {
            Id = 10L,
            Nombre = "Motor Norte",
            Ciudad = "Santander",
            Telefono = "+34 942 000 111",
            Email = "contacto@motornorte.es"
        });
    }

    [Test]
    public void Constructor_ConCadenasVacias_ConservaLasCadenasVacias()
    {
        // Arrange & Act
        var concesionario = new Concesionario(0, string.Empty, string.Empty, string.Empty, string.Empty, false, Fecha, null, null);

        // Assert
        concesionario.Should().BeEquivalentTo(new
        {
            Id = 0L,
            Nombre = string.Empty,
            Ciudad = string.Empty,
            Telefono = string.Empty,
            Email = string.Empty
        });
    }
    
    [Test]
    public void Igualdad_DosInstanciasConMismosValores_SonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario();

        // Act & Assert
        concesionario.Should().Be(otroConcesionario);
    }

    [Test]
    public void Igualdad_ConDistintoId_NoSonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario() with { Id = 11 };

        // Act & Assert
        concesionario.Should().NotBe(otroConcesionario);
    }

    [Test]
    public void Igualdad_ConDistintoNombre_NoSonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario() with { Nombre = "Motor Sur" };

        // Act & Assert
        concesionario.Should().NotBe(otroConcesionario);
    }

    [Test]
    public void Igualdad_ConDistintaCiudad_NoSonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario() with { Ciudad = "Bilbao" };

        // Act & Assert
        concesionario.Should().NotBe(otroConcesionario);
    }

    [Test]
    public void Igualdad_ConDistintoTelefono_NoSonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario() with { Telefono = "+34 942 000 222" };

        // Act & Assert
        concesionario.Should().NotBe(otroConcesionario);
    }

    [Test]
    public void Igualdad_ConDistintoEmail_NoSonIguales()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario() with { Email = "otro@motornorte.es" };

        // Act & Assert
        concesionario.Should().NotBe(otroConcesionario);
    }

    [Test]
    public void Equals_ConNull_DevuelveFalse()
    {
        // Arrange
        var concesionario = CrearConcesionario();

        // Act
        var sonIguales = concesionario.Equals((object?)null);

        // Assert
        sonIguales.Should().BeFalse();
    }

    [Test]
    public void Equals_ConObjetoDeOtroTipo_DevuelveFalse()
    {
        // Arrange
        var concesionario = CrearConcesionario();

        // Act
        var sonIguales = concesionario.Equals((object)"Motor Norte");

        // Assert
        sonIguales.Should().BeFalse();
    }

    [Test]
    public void GetHashCode_DosInstanciasConMismosValores_DevuelveElMismoHash()
    {
        // Arrange
        var concesionario = CrearConcesionario();
        var otroConcesionario = CrearConcesionario();

        // Act
        var hash = concesionario.GetHashCode();
        var otroHash = otroConcesionario.GetHashCode();

        // Assert
        hash.Should().Be(otroHash);
    }
    
    [Test]
    public void With_DevuelveNuevaInstanciaConLosCambios()
    {
        // Arrange
        var original = CrearConcesionario();

        // Act
        var modificado = original with { Ciudad = "Bilbao", Telefono = "+34 944 000 222" };

        // Assert
        modificado.Should().BeEquivalentTo(new
        {
            Id = 10L,
            Nombre = "Motor Norte",
            Ciudad = "Bilbao",
            Telefono = "+34 944 000 222",
            Email = "contacto@motornorte.es"
        });
    }

    [Test]
    public void With_NoModificaLaInstanciaOriginal()
    {
        // Arrange
        var original = CrearConcesionario();

        // Act
        _ = original with { Ciudad = "Bilbao" };

        // Assert
        original.Ciudad.Should().Be("Santander");
    }

    [Test]
    public void Deconstruccion_DevuelveLosValoresEnElOrdenDeclarado()
    {
        // Arrange
        var concesionario = CrearConcesionario();

        // Act
        var (id, nombre, ciudad, telefono, email, isDeleted, createdAt, updatedAt, deletedAt) = concesionario;
        var descompuesto = (Id: id, Nombre: nombre, Ciudad: ciudad, Telefono: telefono, Email: email);

        // Assert
        isDeleted.Should().BeFalse();
        createdAt.Should().Be(Fecha);
        updatedAt.Should().BeNull();
        deletedAt.Should().BeNull();
        descompuesto.Should().Be((10L, "Motor Norte", "Santander", "+34 942 000 111", "contacto@motornorte.es"));
    }

    [Test]
    public void ToString_ContieneTodosLosValores()
    {
        // Arrange
        var concesionario = CrearConcesionario();

        // Act
        var texto = concesionario.ToString();

        // Assert
        texto.Should().ContainAll(
            "10",
            "Motor Norte",
            "Santander",
            "+34 942 000 111",
            "contacto@motornorte.es");
    }
}
