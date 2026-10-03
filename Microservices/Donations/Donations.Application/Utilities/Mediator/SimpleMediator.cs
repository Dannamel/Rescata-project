using Donations.Application.Exceptions;

namespace Donations.Application.Utilities.Mediator;

/// <summary>
/// Mediator mínimo: resuelve desde el contenedor de dependencias el handler registrado
/// para el tipo concreto de la petición y lo ejecuta.
/// </summary>
public sealed class SimpleMediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = ResolveHandler(handlerType, request);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;

        return await (Task<TResponse>)method.Invoke(handler, [request])!;
    }

    public async Task Send(IRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        var handler = ResolveHandler(handlerType, request);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest>.Handle))!;

        await (Task)method.Invoke(handler, [request])!;
    }

    private object ResolveHandler(Type handlerType, object request) =>
        serviceProvider.GetService(handlerType)
        ?? throw new MediatorException($"No se encontró un handler registrado para {request.GetType().Name}.");
}
