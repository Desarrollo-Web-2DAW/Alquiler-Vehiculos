using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Errors.Common;
using Alquiler_Vehiculos.Errors.Reserva;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Reserva;

/// <summary>
/// Máquina de estados pura de una reserva (RF-31, RF-32, CU-21, RF-29).
/// Valida transiciones sin saltar estados y centraliza qué estados son vivos, finales o modificables.
/// </summary>
public static class ReservaEstadoServices {
    private static readonly Dictionary<EstadoReserva, EstadoReserva[]> TransicionesValidas = new() {
        [EstadoReserva.Pendiente] = [EstadoReserva.Confirmada, EstadoReserva.Caducada, EstadoReserva.Cancelada],
        [EstadoReserva.Confirmada] = [EstadoReserva.EnCurso, EstadoReserva.Cancelada, EstadoReserva.NoPresentado],
        [EstadoReserva.EnCurso] = [EstadoReserva.Finalizada],
        [EstadoReserva.Finalizada] = [],
        [EstadoReserva.Caducada] = [],
        [EstadoReserva.Cancelada] = [],
        [EstadoReserva.NoPresentado] = []
    };

    /// <summary>Comprueba si la transición está permitida o devuelve 409 Conflict (RF-32).</summary>
    public static UnitResult<DomainError> ValidarTransicion(EstadoReserva origen, EstadoReserva destino) {
        if (TransicionesValidas.TryGetValue(origen, out var permitidos) && permitidos.Contains(destino)) {
            return UnitResult.Success<DomainError>();
        }

        return UnitResult.Failure<DomainError>(ReservaError.TransicionProhibida(origen, destino));
    }

    /// <summary>CU-21: solo se pueden modificar fechas en Pendiente o Confirmada; el resto da 409.</summary>
    public static UnitResult<DomainError> ValidarModificable(EstadoReserva estadoActual) {
        return estadoActual is EstadoReserva.Pendiente or EstadoReserva.Confirmada
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(ReservaError.NoModificable(estadoActual));
    }

    /// <summary>RF-29: solo se puede valorar cuando la reserva está Finalizada; el resto da 409.</summary>
    public static UnitResult<DomainError> ValidarValorable(EstadoReserva estadoActual) {
        return estadoActual == EstadoReserva.Finalizada
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(ReservaError.SoloFinalizadaParaValorar(estadoActual));
    }

    /// <summary>Estados en los que la reserva sigue viva y retiene disponibilidad (RF-01, RF-07, RF-13).</summary>
    public static bool EsViva(EstadoReserva estado) =>
        estado is EstadoReserva.Pendiente or EstadoReserva.Confirmada or EstadoReserva.EnCurso;

    /// <summary>Estados terminales que nunca vuelven a transicionar.</summary>
    public static bool EsFinal(EstadoReserva estado) =>
        TransicionesValidas[estado].Length == 0;

    /// <summary>
    /// RF-33: calcula el estado de vehículo resultante tras una transición de reserva.
    /// Devuelve <c>null</c> si el vehículo no debe cambiar de estado (ej. estaba EnAlquiler al crear una reserva futura).
    /// </summary>
    public static string? CalcularNuevoEstadoVehiculo(
        EstadoReserva nuevoEstadoReserva,
        string estadoActualVehiculo,
        bool tieneOtrasReservasVivas) {
        return nuevoEstadoReserva switch {
            EstadoReserva.Pendiente => estadoActualVehiculo == "Disponible" ? "Reservado" : null,
            EstadoReserva.EnCurso => "EnAlquiler",
            EstadoReserva.Cancelada or EstadoReserva.Caducada or EstadoReserva.NoPresentado =>
                estadoActualVehiculo == "Reservado" && !tieneOtrasReservasVivas ? "Disponible" : null,
            EstadoReserva.Finalizada => tieneOtrasReservasVivas ? "Reservado" : "Disponible",
            _ => null
        };
    }
}