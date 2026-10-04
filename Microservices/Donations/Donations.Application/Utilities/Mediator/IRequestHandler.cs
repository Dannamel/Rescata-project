namespace Donations.Application.Utilities.Mediator;

/// <summary>Maneja una petición que devuelve <typeparamref name="TResponse"/>.</summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request);
}

/// <summary>Maneja una petición sin respuesta.</summary>
public interface IRequestHandler<TRequest>
    where TRequest : IRequest
{
    Task Handle(TRequest request);
}
