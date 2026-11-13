using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;
using TicketReservation.Domain;

namespace TicketReservation.Features.Reservations;

/// <summary>Slice: rezervasyonu onayla (ödeme alındı varsayımıyla). Koltuk Sold olur.</summary>
public static class ConfirmReservation
{
    public record Command(Guid ReservationId) : ICommand<Result<Guid>>;

    public class Handler(AppDbContext db, TimeProvider clock) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command cmd, CancellationToken ct)
        {
            var now = clock.GetUtcNow().UtcDateTime;

            var reservation = await db.Reservations.SingleOrDefaultAsync(r => r.Id == cmd.ReservationId, ct);
            if (reservation is null)
                return DomainErrors.ReservationNotFound;

            if (reservation.Status != ReservationStatus.Pending)
                return DomainErrors.ReservationNotPending;

            if (reservation.IsExpiredAt(now))
            {
                // Koltuğa dokunmuyoruz: süre dolduktan sonra başkası almış olabilir.
                reservation.MarkExpired();
                await db.SaveChangesAsync(ct);
                return DomainErrors.ReservationExpired;
            }

            var seat = await db.Seats.SingleAsync(s => s.Id == reservation.SeatId, ct);

            reservation.Confirm();
            seat.MarkSold();

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainErrors.SeatUnavailable;
            }

            return reservation.Id;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/reservations/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new Command(id), ct)).ToHttp(_ => Results.NoContent()))
                .WithTags("Reservations");
    }
}
