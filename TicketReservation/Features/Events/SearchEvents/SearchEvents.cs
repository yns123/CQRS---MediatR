using Dapper;
using FluentValidation;
using MediatR;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;

namespace TicketReservation.Features.Events;

/// <summary>Slice: etkinlik ara. Okuma tarafı: EF yok, doğrudan Dapper + ham SQL, DTO'ya direkt projeksiyon.</summary>
public static class SearchEvents
{
    public record Query(string? Search, int Page, int PageSize) : IQuery<Result<IReadOnlyList<EventListItem>>>;

    public class EventListItem
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string Venue { get; init; } = default!;
        public DateTime StartsAt { get; init; }
        public int TotalSeats { get; init; }
        public int AvailableSeats { get; init; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class Handler(ISqlConnectionFactory connections, TimeProvider clock)
        : IRequestHandler<Query, Result<IReadOnlyList<EventListItem>>>
    {
        public async Task<Result<IReadOnlyList<EventListItem>>> Handle(Query q, CancellationToken ct)
        {
            var sql = $"""
                SELECT e.Id, e.Name, e.Venue, e.StartsAt,
                       COUNT(s.Id) AS TotalSeats,
                       SUM(CASE WHEN {SeatSql.EffectiveStatus("s")} = 0 THEN 1 ELSE 0 END) AS AvailableSeats
                FROM Events e
                JOIN Seats s ON s.EventId = e.Id
                WHERE (@Search IS NULL OR e.Name LIKE '%' + @Search + '%')
                GROUP BY e.Id, e.Name, e.Venue, e.StartsAt
                ORDER BY e.StartsAt
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
                """;

            await using var conn = connections.Create();
            var rows = await conn.QueryAsync<EventListItem>(new CommandDefinition(
                sql,
                new { q.Search, Now = clock.GetUtcNow().UtcDateTime, Skip = (q.Page - 1) * q.PageSize, Take = q.PageSize },
                cancellationToken: ct));

            return rows.ToList();
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app) =>
            app.MapGet("/events", async (string? search, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new Query(search, page ?? 1, pageSize ?? 20), ct)).ToHttp(Results.Ok))
                .WithTags("Events");
    }
}
