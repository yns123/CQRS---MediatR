namespace TicketReservation.Common.Outcomes;

public enum ErrorType { Failure, NotFound, Conflict }

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}

/// <summary>
/// Beklenen iş hatalarını (bulunamadı, çakışma...) exception yerine değer olarak döndürür.
/// Exception'lar sadece beklenmeyen durumlar ve doğrulama hataları için kalır.
/// </summary>
public sealed class Result<T>
{
    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(Error error) { Error = error; }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : result.Error!.ToProblem();

    private static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: status);
    }
}
