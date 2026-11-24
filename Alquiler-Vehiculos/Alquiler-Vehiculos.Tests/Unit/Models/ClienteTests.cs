using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Models;

/// <summary>
/// Tests unitarios de las reglas de edad del cliente.
/// Requisito cubierto: RF-13. La edad se mide el día de la recogida: quien cumple 18 ese día
/// puede alquilar, y el recargo de conductor joven es para quien aún no tiene 25 ese día.
/// </summary>
[Category("Unit")]
public class ClienteTests {
    private static readonly DateOnly DiaDeRecogida = new(2026, 11, 21);

    private static Cliente ClienteNacidoEl(int anio, int mes, int dia) => new() {
        FechaNacimiento = new DateOnly(anio, mes, dia)
    };

    [TestFixture]
    public class CasosPositivos {
        /// <summary>
        /// Dada una fecha de nacimiento y una fecha de referencia, cuando se calcula la edad cumplida,
        /// entonces cuenta los años completos, y cumplir años ese mismo día ya cuenta.
        /// </summary>
        [TestCase(1995, 3, 21, 2026, 11, 21, 31)]
        [TestCase(1995, 12, 25, 2026, 11, 21, 30)]
        [TestCase(2008, 11, 21, 2026, 11, 21, 18)]
        [TestCase(2008, 11, 22, 2026, 11, 21, 17)]
        [TestCase(2008, 2, 29, 2026, 2, 28, 18)]
        [TestCase(2008, 2, 29, 2026, 2, 27, 17)]
        public void EdadCumplida_FechaDeReferencia_DevuelveLosAniosCompletos(
            int anioNac, int mesNac, int diaNac, int anioRef, int mesRef, int diaRef, int esperada) {
            // Arrange
            var cliente = ClienteNacidoEl(anioNac, mesNac, diaNac);

            // Act
            var edad = cliente.EdadCumplida(new DateOnly(anioRef, mesRef, diaRef));

            // Assert
            edad.Should().Be(esperada);
        }

        /// <summary>
        /// Dado un cliente que cumple 18 el día de la recogida o es mayor, cuando se comprueba
        /// si es mayor de edad, entonces lo es.
        /// </summary>
        [TestCase(2008, 11, 21)]
        [TestCase(2000, 1, 1)]
        public void EsMayorDeEdad_CumpleDieciochoOMas_DevuelveTrue(int anio, int mes, int dia) {
            // Arrange
            var cliente = ClienteNacidoEl(anio, mes, dia);

            // Act
            var esMayor = cliente.EsMayorDeEdad(DiaDeRecogida);

            // Assert
            esMayor.Should().BeTrue();
        }

        /// <summary>
        /// Dado un cliente que el día de la recogida aún no tiene 25, cuando se comprueba
        /// si es conductor joven, entonces lo es y paga el recargo.
        /// </summary>
        [TestCase(2001, 11, 22)]
        [TestCase(2008, 11, 21)]
        public void EsConductorJoven_MenosDeVeinticinco_DevuelveTrue(int anio, int mes, int dia) {
            // Arrange
            var cliente = ClienteNacidoEl(anio, mes, dia);

            // Act
            var esJoven = cliente.EsConductorJoven(DiaDeRecogida);

            // Assert
            esJoven.Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos {
        /// <summary>
        /// Dado un cliente al que le falta algún día para cumplir 18 el día de la recogida,
        /// cuando se comprueba si es mayor de edad, entonces no lo es.
        /// </summary>
        [TestCase(2008, 11, 22)]
        [TestCase(2015, 5, 5)]
        public void EsMayorDeEdad_AunNoCumpleDieciocho_DevuelveFalse(int anio, int mes, int dia) {
            // Arrange
            var cliente = ClienteNacidoEl(anio, mes, dia);

            // Act
            var esMayor = cliente.EsMayorDeEdad(DiaDeRecogida);

            // Assert
            esMayor.Should().BeFalse();
        }

        /// <summary>
        /// Dado un cliente que cumple 25 el día de la recogida o es mayor, cuando se comprueba
        /// si es conductor joven, entonces no lo es y no paga recargo.
        /// </summary>
        [TestCase(2001, 11, 21)]
        [TestCase(1990, 1, 1)]
        public void EsConductorJoven_VeinticincoOMas_DevuelveFalse(int anio, int mes, int dia) {
            // Arrange
            var cliente = ClienteNacidoEl(anio, mes, dia);

            // Act
            var esJoven = cliente.EsConductorJoven(DiaDeRecogida);

            // Assert
            esJoven.Should().BeFalse();
        }
    }
}