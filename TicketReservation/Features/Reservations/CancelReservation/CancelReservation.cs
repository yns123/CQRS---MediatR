using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;
using TicketReservation.Domain;

namespace TicketReservation.Features.Reservations;

/// <summary>Slice: rezervasyonu iptal et. Bekleyen veya onaylı olabilir; koltuk hâlâ bizdeyse serbest kalır.</summary>
public static class CancelReservation
{
    public record Command(Guid ReservationId) : ICommand<Result<Guid>>;

    public class Handler(AppDbContext db) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command cmd, CancellationToken ct)
        {
            var reservation = await db.Reservations.SingleOrDefaultAsync(r => r.Id == cmd.ReservationId, ct);
            if (reservation is null)
                return DomainErrors.ReservationNotFound;

            if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
                return DomainErrors.ReservationNotPending;

            var seat = await db.Seats.SingleAsync(s => s.Id == reservation.SeatId, ct);

            // Önce koltuk kontrolü (rezervasyon durumu değişmeden): başkasının tuttuğu koltuğu serbest bırakma.
            if (seat.IsHeldBy(reservation))
                seat.Release();

            reservation.Cancel();

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
            app.MapDelete("/reservations/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new Command(id), ct)).ToHttp(_ => Results.NoContent()))
                .WithTags("Reservations");
    }
}
