using FluentValidation;
using MediatR;
using TicketReservation.Common.Abstractions;
using TicketReservation.Common.Persistence;
using TicketReservation.Common.Outcomes;
using TicketReservation.Domain;

namespace TicketReservation.Features.Events;

/// <summary>Slice: etkinlik oluştur. Command, validator, handler ve endpoint tek dosyada.</summary>
public static class CreateEvent
{
    public record Command(string Name, string Venue, DateTime StartsAt, int SeatCount) : ICommand<Result<Guid>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider clock)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
            RuleFor(x => x.StartsAt).GreaterThan(_ => clock.GetUtcNow().UtcDateTime)
                .WithMessage("Etkinlik tarihi gelecekte olmalı.");
            RuleFor(x => x.SeatCount).InclusiveBetween(1, 5000);
        }
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command cmd, CancellationToken ct)
        {
            var ev = Event.Create(cmd.Name, cmd.Venue, cmd.StartsAt, cmd.SeatCount);
            db.Events.Add(ev);
            await db.SaveChangesAsync(ct);
            return ev.Id;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app) =>
            app.MapPost("/events", async (Command cmd, ISender sender, CancellationToken ct) =>
                    (await sender.Send(cmd, ct)).ToHttp(id => Results.Created($"/events/{id}/seats", new { id })))
                .WithTags("Events");
    }
}
