using FluentValidation;
using MediatR;

namespace TicketReservation.Common.Behaviors;

/// <summary>Pipeline: handler çalışmadan önce, o istek için yazılmış tüm validator'ları çalıştırır.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}

/// <summary>ValidationException'ı 400 + ValidationProblemDetails'e çevirir.</summary>
public sealed class ValidationExceptionHandler : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        await Results.ValidationProblem(errors).ExecuteAsync(httpContext);
        return true;
    }
}
