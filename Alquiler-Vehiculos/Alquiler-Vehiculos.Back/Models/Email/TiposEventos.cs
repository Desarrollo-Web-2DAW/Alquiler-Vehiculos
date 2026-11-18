namespace Alquiler_Vehiculos.Models.Email;

/// <summary>
///     Motivo de un reembolso enviado al cliente (RF-34).
/// </summary>
public enum TipoReembolso {
    /// <summary>Cancelación de una reserva pagada (CU-09).</summary>
    AnulacionReserva,

    /// <summary>Penalización automática por no presentarse a la recogida (CU-13).</summary>
    PenalizacionNoPresentado
}

/// <summary>
///     Solución aplicada a la reserva cuando el vehículo entra al taller (RF-07).
/// </summary>
public enum TipoAfectacionTaller {
    /// <summary>Se asigna un vehículo equivalente de la misma categoría.</summary>
    CambioVehiculo,

    /// <summary>Se cancela la reserva (con reembolso si estaba pagada).</summary>
    CancelacionReserva,

    /// <summary>Se aplaza la recogida a otras fechas acordadas con el cliente.</summary>
    Aplazamiento
}

/// <summary>
///     Solución aplicada a un alquiler en curso con avería (CU-12).
/// </summary>
public enum SolucionAveria {
    /// <summary>Fin anticipado del alquiler con devolución del importe no disfrutado.</summary>
    FinAnticipado,

    /// <summary>Se entrega un vehículo de sustitución.</summary>
    VehiculoSustitucion,

    /// <summary>Se aplaza la devolución hasta que el vehículo esté reparado.</summary>
    AplazamientoDevolucion
}
