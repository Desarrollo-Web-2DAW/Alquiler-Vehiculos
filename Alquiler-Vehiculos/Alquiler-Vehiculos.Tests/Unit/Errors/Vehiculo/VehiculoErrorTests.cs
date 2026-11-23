using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Vehiculo;
using Alquiler_Vehiculos.Models.Vehiculo.Enums;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Errors.Vehiculo;

[TestFixture]
[Category("Unit")]
public class VehiculoErrorTests {
    private const string Id = "dQw4w9WgXcQ";
    private const string Matricula = "1234BCD";

    public static IEnumerable<TestCaseData> Errores() {
        yield return Caso(() => VehiculoError.NotFoundVehiculo(Id),
            new NotFoundError("VEHICULO_NO_ENCONTRADO", $"Vehículo con ID '{Id}' no encontrado"), "VehiculoPorId");
        yield return Caso(() => VehiculoError.NotFoundVehiculoMatricula(Matricula),
            new NotFoundError("VEHICULO_NO_ENCONTRADO", $"Vehículo con matrícula '{Matricula}' no encontrado"), "VehiculoPorMatricula");
        yield return Caso(() => VehiculoError.VehiculoYaExiste(Matricula),
            new ConflictError("MATRICULA_DUPLICADA", $"Ya existe un vehículo con matrícula '{Matricula}'"), "MatriculaDuplicada");
        yield return Caso(() => VehiculoError.TipoDeCarroceria(),
            ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Categoría no válido"), "TipoCarroceria");
        yield return Caso(() => VehiculoError.TipoCombustible(),
            ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Combustible no válido"), "TipoCombustible");
        yield return Caso(() => VehiculoError.TipoDeTransmision(),
            ValidationError.Create("El Acriss dado no es correcto respecto al catálogo, tipo de Transmision no válido"), "TipoTransmision");
        yield return Caso(() => VehiculoError.TipoColorInvalido(),
            ValidationError.Create("El color especificado no es válido en el catálogo."), "TipoColor");
        yield return Caso(() => VehiculoError.CambiarDeEstado("Disponible", "Reservado"),
            new ConflictError("ESTADO_VEHICULO_INVALIDO",
                "No se puede cambiar el estado del vehiculo debido a restricciones de negocio: EstadoActual:Disponible-EstadoPedido:Reservado"), "CambiarEstado");
        yield return Caso(() => VehiculoError.NoDisponibleParaBaja(EstadoVehiculo.EnAlquiler),
            new ConflictError("VEHICULO_NO_DISPONIBLE",
                "Solo se puede dar de baja un vehículo Disponible. Estado actual: EnAlquiler"), "NoDisponibleParaBaja");
        yield return Caso(() => VehiculoError.VehiculoConPartesAbiertos(Id),
            new ConflictError("VEHICULO_CON_PARTE_ABIERTO",
                $"No se puede dar de baja el vehículo '{Id}' porque tiene un parte de taller abierto."), "PartesAbiertos");
        yield return Caso(() => VehiculoError.BorradoError(),
            new BusinessRuleError("BORRADO_VEHICULO", "No se ha podido completar la operación de borrado del vehículo."), "Borrado");
    }

    private static TestCaseData Caso(Func<DomainError> crear, DomainError esperado, string nombre) =>
        new TestCaseData(crear, esperado).SetName($"Factoria_{nombre}_ConservaTipoCodigoYMensaje");
}
