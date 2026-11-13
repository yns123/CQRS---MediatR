namespace TicketReservation.Domain;

public enum SeatStatus { Available = 0, Reserved = 1, Sold = 2 }

public class Seat
{
    private Seat() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public int Number { get; private set; }
    public SeatStatus Status { get; private set; }
    public DateTime? ReservedUntil { get; private set; }

    /// <summary>Optimistic concurrency: aynı koltuğa aynı anda iki kişi yazarsa ikincisi DbUpdateConcurrencyException alır.</summary>
    public byte[] RowVersion { get; private set; } = default!;

    public static Seat Create(Guid eventId, int number) =>
        new() { Id = Guid.NewGuid(), EventId = eventId, Number = number, Status = SeatStatus.Available };

    /// <summary>Süresi dolmuş bir rezervasyon, koltuğu yeniden alınabilir yapar (ayrı bir job gerekmez).</summary>
    public bool IsAvailableAt(DateTime now) =>
        Status == SeatStatus.Available || (Status == SeatStatus.Reserved && ReservedUntil <= now);

    /// <summary>Koltuk hâlâ bu rezervasyonun tuttuğu koltuk mu? (Başkası aldıysa dokunmamalıyız.)</summary>
    public bool IsHeldBy(Reservation reservation) =>
        reservation.Status == ReservationStatus.Confirmed
            ? Status == SeatStatus.Sold
            : Status == SeatStatus.Reserved && ReservedUntil == reservation.ExpiresAt;

    public void Reserve(DateTime until)
    {
        Status = SeatStatus.Reserved;
        ReservedUntil = until;
    }

    public void MarkSold()
    {
        Status = SeatStatus.Sold;
        ReservedUntil = null;
    }

    public void Release()
    {
        Status = SeatStatus.Available;
        ReservedUntil = null;
    }
}
