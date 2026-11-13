namespace TicketReservation.Domain;

public class Event
{
    private Event() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Venue { get; private set; } = default!;
    public DateTime StartsAt { get; private set; }
    public List<Seat> Seats { get; private set; } = [];

    public static Event Create(string name, string venue, DateTime startsAt, int seatCount)
    {
        var id = Guid.NewGuid();
        return new Event
        {
            Id = id,
            Name = name,
            Venue = venue,
            StartsAt = startsAt,
            Seats = Enumerable.Range(1, seatCount).Select(n => Seat.Create(id, n)).ToList()
        };
    }
}
