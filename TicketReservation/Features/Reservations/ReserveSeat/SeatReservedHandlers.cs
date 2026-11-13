using MediatR;

namespace TicketReservation.Features.Reservations;

/// <summary>Aynı Notification'a birden fazla handler abone olabilir. Biri patlarsa diğerleri etkilenir mi? Deneyin.</summary>
public class SendReservationEmailHandler(ILogger<SendReservationEmailHandler> logger)
    : INotificationHandler<SeatReservedNotification>
{
    public Task Handle(SeatReservedNotification n, CancellationToken ct)
    {
        logger.LogInformation("[SİMÜLE MAİL] {Email}: {Seat} numaralı koltuk 10 dk tutuldu (rezervasyon {Id})",
            n.CustomerEmail, n.SeatNumber, n.ReservationId);
        return Task.CompletedTask;
    }
}

public class TrackReservationMetricsHandler(ILogger<TrackReservationMetricsHandler> logger)
    : INotificationHandler<SeatReservedNotification>
{
    public Task Handle(SeatReservedNotification n, CancellationToken ct)
    {
        logger.LogInformation("[SİMÜLE METRİK] event={EventId} için yeni rezervasyon", n.EventId);
        return Task.CompletedTask;
    }
}
