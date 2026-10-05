using System.Reflection;
using Donations.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace Donations.Application.Utilities.Mediator;

/// <summary>
/// Mediator mínimo: resuelve desde el contenedor de dependencias el handler registrado
/// para el tipo concreto de la petición y lo ejecuta.
/// Antes de ejecutarlo, valida la petición si tiene un validador de FluentValidation registrado.
/// </summary>
public sealed class SimpleMediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ValidateRequestAsync(request).ConfigureAwait(false);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = ResolveHandler(handlerType, request);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;

        return await (Task<TResponse>)method.Invoke(handler, [request])!;
    }

    public async Task Send(IRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ValidateRequestAsync(request).ConfigureAwait(false);

        var handlerType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        var handler = ResolveHandler(handlerType, request);
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest>.Handle))!;

        await (Task)method.Invoke(handler, [request])!;
    }

    private object ResolveHandler(Type handlerType, object request) =>
        serviceProvider.GetService(handlerType)
        ?? throw new MediatorException($"No se encontró un handler registrado para {request.GetType().Name}.");

    private async Task ValidateRequestAsync(object request)
    {
        Type requestType = request.GetType();
        Type validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
        object? validator = serviceProvider.GetService(validatorInterface);

        // Si el caso de uso no tiene validador registrado, no hay nada que validar.
        if (validator is null)
        {
            return;
        }

        MethodInfo? validateMethod = validatorInterface.GetMethod("ValidateAsync", [requestType, typeof(CancellationToken)]);

        if (validateMethod is null)
        {
            return;
        }

        object? validationResult = validateMethod.Invoke(validator, [request, CancellationToken.None]);

        if (validationResult is not Task task)
        {
            return;
        }

        await task.ConfigureAwait(false);

        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");

        if (resultProperty?.GetValue(task) is not ValidationResult result)
        {
            return;
        }

        if (!result.IsValid)
        {
            throw new CustomValidationException(result);
        }
    }
}
