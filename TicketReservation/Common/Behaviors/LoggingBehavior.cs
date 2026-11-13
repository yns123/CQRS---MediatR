using System.Diagnostics;
using MediatR;

namespace TicketReservation.Common.Behaviors;

/// <summary>Pipeline: her istek öncesi/sonrası log + süre ölçümü.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).FullName?.Split('.').Last().Replace('+', '.') ?? typeof(TRequest).Name;
        logger.LogInformation("→ {Request} başladı", name);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            logger.LogInformation("← {Request} bitti ({Elapsed} ms)", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("✗ {Request} hata verdi ({Elapsed} ms): {Message}", name, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
