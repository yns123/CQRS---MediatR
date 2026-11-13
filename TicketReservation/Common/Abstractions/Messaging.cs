using MediatR;

namespace TicketReservation.Common.Abstractions;

/// <summary>Durumu değiştiren istek (CQRS "C"). Yan etkisi vardır.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>Sadece veri okuyan istek (CQRS "Q"). Yan etkisi olmamalıdır.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
