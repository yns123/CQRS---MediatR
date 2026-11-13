namespace TicketReservation.Domain;

public enum ReservationStatus { Pending = 0, Confirmed = 1, Cancelled = 2, Expired = 3 }

public class Reservation
{
    private Reservation() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid SeatId { get; private set; }
    public string CustomerEmail { get; private set; } = default!;
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public static Reservation Create(Seat seat, string customerEmail, DateTime now, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        EventId = seat.EventId,
        SeatId = seat.Id,
        CustomerEmail = customerEmail,
        Status = ReservationStatus.Pending,
        CreatedAt = now,
        ExpiresAt = expiresAt
    };

    public bool IsExpiredAt(DateTime now) => ExpiresAt <= now;

    public void Confirm() => Status = ReservationStatus.Confirmed;
    public void Cancel() => Status = ReservationStatus.Cancelled;
    public void MarkExpired() => Status = ReservationStatus.Expired;
}
