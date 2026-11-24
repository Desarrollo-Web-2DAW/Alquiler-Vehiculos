using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Pago;

/// <summary>
/// Errores de dominio del módulo de pagos con tarjeta (RF-21, RF-22, CU-11, CU-20).
/// </summary>
public static class PagoError {
    public static ValidationError MedioNoAdmitido() =>
        ValidationError.Create("Solo se admite el pago con tarjeta de crédito");

    public static ValidationError TarjetaInvalida() =>
        ValidationError.Create("El número de tarjeta no supera la validación de Luhn");

    public static ValidationError TarjetaCaducada() =>
        ValidationError.Create("La tarjeta de crédito está caducada");

    public static ConflictError ReservaYaPagada(Guid reservaId) =>
        new("RESERVA_YA_PAGADA",
            $"La reserva con ID '{reservaId}' ya tiene un pago aprobado");

    public static BusinessRuleError PagoRechazado(string motivo) =>
        new("PAGO_RECHAZADO", motivo);
}