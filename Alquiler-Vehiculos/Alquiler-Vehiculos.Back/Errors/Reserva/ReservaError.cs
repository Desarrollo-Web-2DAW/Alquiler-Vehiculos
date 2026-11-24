using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Errors.Common;

namespace Alquiler_Vehiculos.Errors.Reserva;

/// <summary>
/// Errores de dominio del módulo de reservas y valoraciones (RF-11, RF-13, RF-29, RF-30, RF-32, CU-21).
/// </summary>
public static class ReservaError {
    public static NotFoundError NotFound(Guid id) =>
        NotFoundError.FromId(id, "Reserva", "RESERVA_NO_ENCONTRADA");

    public static ConflictError TransicionProhibida(EstadoReserva origen, EstadoReserva destino) =>
        new("TRANSICION_ESTADO_PROHIBIDA",
            $"No se permite pasar una reserva de '{origen}' a '{destino}'");

    public static ConflictError NoModificable(EstadoReserva estadoActual) =>
        
        
        new("RESERVA_NO_MODIFICABLE",
            $"No se pueden modificar las fechas de una reserva en estado '{estadoActual}'");

    public static ConflictError VehiculoNoDisponible(string mensaje) =>
        new("VEHICULO_NO_DISPONIBLE", mensaje);

    public static ValidationError FueraDeHorarioOficina() =>
        ValidationError.Create("La recogida y la devolución deben estar dentro del horario de la oficina");

    public static ValidationError ClienteMenorDeEdad() =>
        ValidationError.Create("El cliente debe ser mayor de 18 años el día de la recogida");

    public static ConflictError SoloFinalizadaParaValorar(EstadoReserva estadoActual) =>
        new("RESERVA_NO_FINALIZADA",
            $"Solo se puede valorar una reserva finalizada (estado actual: '{estadoActual}')");

    public static ForbiddenError ValoracionDeOtroCliente() =>
        new("VALORACION_AJENA", "No puedes valorar una reserva que pertenece a otro cliente");

    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}