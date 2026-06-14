using EventosVivos.Domain.Enums;

namespace EventosVivos.Application.DTOs;

public static class ReservationStatusApiMapper
{
    public static string ToApiString(ReservationStatus status) => status switch
    {
        ReservationStatus.PendientePago => "pendiente_pago",
        ReservationStatus.Confirmada => "confirmada",
        ReservationStatus.Cancelada => "cancelada",
        ReservationStatus.Perdida => "perdida",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown reservation status.")
    };
}
