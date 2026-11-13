using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;
using TicketReservation.Domain;

namespace TicketReservation.Features.Reservations;

/// <summary>Slice: koltuk tut. Projenin kalbi: eşzamanlılık + domain event (Notification).</summary>
public static class ReserveSeat
{
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    public record Command(Guid EventId, int SeatNumber, string CustomerEmail) : ICommand<Result<Response>>;

    public record Response(Guid ReservationId, DateTime ExpiresAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EventId).NotEmpty();
            RuleFor(x => x.SeatNumber).GreaterThan(0);
            RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(320);
        }
    }

    public class Handler(AppDbContext db, TimeProvider clock, IPublisher publisher)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var now = clock.GetUtcNow().UtcDateTime;

            var seat = await db.Seats.SingleOrDefaultAsync(
                s => s.EventId == cmd.EventId && s.Number == cmd.SeatNumber, ct);

            if (seat is null)
                return DomainErrors.SeatNotFound;

            if (!seat.IsAvailableAt(now))
                return DomainErrors.SeatUnavailable;

            var expiresAt = now + HoldDuration;
            seat.Reserve(expiresAt);

            var reservation = Reservation.Create(seat, cmd.CustomerEmail, now, expiresAt);
            db.Reservations.Add(reservation);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Araya başka biri girip koltuğu aldı: RowVersion uyuşmadı. Kaybeden taraf 409 alır.
                return DomainErrors.SeatUnavailable;
            }

            // Domain event: birden fazla handler'a (mail, istatistik...) yayınlanır; Command bunları bilmez.
            await publisher.Publish(
                new SeatReservedNotification(reservation.Id, cmd.EventId, cmd.SeatNumber, cmd.CustomerEmail), ct);

            return new Response(reservation.Id, expiresAt);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/reservations", async (Command cmd, ISender sender, CancellationToken ct) =>
                    (await sender.Send(cmd, ct)).ToHttp(r => Results.Created($"/reservations/{r.ReservationId}", r)))
                .WithTags("Reservations");
    }
}

public record SeatReservedNotification(Guid ReservationId, Guid EventId, int SeatNumber, string CustomerEmail)
    : INotification;
