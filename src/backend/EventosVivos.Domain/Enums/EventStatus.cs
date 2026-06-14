namespace EventosVivos.Domain.Enums;

/// <summary>
/// Public-facing event status. Actual persistence stores only the IsCanceled flag;
/// completado is derived from schedule + clock.
/// </summary>
public enum EventStatus
{
    Activo,
    Cancelado,
    Completado
}
