using TicketReservation.Common.Outcomes;

namespace TicketReservation.Domain;

/// <summary>Birden fazla slice'ın paylaştığı hatalar. Sadece tekrar eden şeyler buraya çıkar.</summary>
public static class DomainErrors
{
    public static readonly Error EventNotFound =
        Error.NotFound("Event.NotFound", "Etkinlik bulunamadı.");

    public static readonly Error SeatNotFound =
        Error.NotFound("Seat.NotFound", "Koltuk bulunamadı.");

    public static readonly Error SeatUnavailable =
        Error.Conflict("Seat.Unavailable", "Koltuk müsait değil.");

    public static readonly Error ReservationNotFound =
        Error.NotFound("Reservation.NotFound", "Rezervasyon bulunamadı.");

    public static readonly Error ReservationNotPending =
        Error.Conflict("Reservation.NotPending", "Rezervasyon bu işlem için uygun durumda değil.");

    public static readonly Error ReservationExpired =
        Error.Conflict("Reservation.Expired", "Rezervasyonun süresi dolmuş.");
}
