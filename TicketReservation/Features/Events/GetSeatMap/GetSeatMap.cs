using Dapper;
using MediatR;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;
using TicketReservation.Domain;

namespace TicketReservation.Features.Events;

/// <summary>Slice: koltuk haritası. İki sonuç kümesi tek round-trip'te (QueryMultiple).</summary>
public static class GetSeatMap
{
    public record Query(Guid EventId) : IQuery<Result<Response>>;

    public record SeatDto(int Number, SeatStatus Status);

    public record Response(Guid EventId, string Name, string Venue, DateTime StartsAt, IReadOnlyList<SeatDto> Seats);

    private class EventHeader
    {
        public string Name { get; init; } = default!;
        public string Venue { get; init; } = default!;
        public DateTime StartsAt { get; init; }
    }

    private class SeatRow
    {
        public int Number { get; init; }
        public SeatStatus Status { get; init; }
    }

    public class Handler(ISqlConnectionFactory connections, TimeProvider clock) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query q, CancellationToken ct)
        {
            var sql = $"""
                SELECT Name, Venue, StartsAt FROM Events WHERE Id = @EventId;

                SELECT s.Number, {SeatSql.EffectiveStatus("s")} AS Status
                FROM Seats s
                WHERE s.EventId = @EventId
                ORDER BY s.Number;
                """;

            await using var conn = connections.Create();
            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(
                sql,
                new { q.EventId, Now = clock.GetUtcNow().UtcDateTime },
                cancellationToken: ct));

            var header = await grid.ReadSingleOrDefaultAsync<EventHeader>();
            if (header is null)
                return DomainErrors.EventNotFound;

            var seats = (await grid.ReadAsync<SeatRow>()).Select(s => new SeatDto(s.Number, s.Status)).ToList();
            return new Response(q.EventId, header.Name, header.Venue, header.StartsAt, seats);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app) =>
            app.MapGet("/events/{eventId:guid}/seats", async (Guid eventId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new Query(eventId), ct)).ToHttp(Results.Ok))
                .WithTags("Events");
    }
}
