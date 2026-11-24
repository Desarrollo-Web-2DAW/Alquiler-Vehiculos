

namespace Alquiler_Vehiculos.Config;

/// <summary>
/// Constantes de negocio del módulo de reservas, pagos y valoraciones (RI-01).
/// </summary>
public static class ReservasConstantes {
    public const int DuracionMinimoDias = 1;
    public const int DuracionMaximoDias = 30;
    public const int MinutosCaducidadPendiente = 30;
    public const int EdadMinimaAlquiler = 18;
    public const int EdadSinRecargoJoven = 25;
    public const decimal PorcentajeRecargoConductorJoven = 0.25m;
    public const int DiasPenalizacionNoPresentado = 1;
    public const string DivisaPorDefecto = "EUR";
    public const int PuntuacionMinima = 1;
    public const int PuntuacionMaxima = 5;
}