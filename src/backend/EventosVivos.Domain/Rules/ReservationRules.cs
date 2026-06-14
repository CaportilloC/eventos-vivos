namespace EventosVivos.Domain.Rules;

public static class ReservationRules
{
    public const int PendingExpirationMinutes = 15;
    public const int LatestReservationHoursBeforeStart = 1;
    public const int LastDayWindowHours = 24;
    public const int LastDayMaxTickets = 5;
    public const int LateCancellationPenaltyHours = 48;
    public const decimal HighPriceThreshold = 100m;
    public const int HighPriceMaxTickets = 10;
    public const int MaxTicketsPerRequest = 100;
    public const int BuyerNameMinLength = 2;
    public const int BuyerNameMaxLength = 100;
    public const int BuyerEmailMaxLength = 200;
}
