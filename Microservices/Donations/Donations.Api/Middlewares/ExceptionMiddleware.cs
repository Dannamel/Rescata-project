using System.Reflection;
using Donations.Application.Exceptions;
using Donations.Domain.Exceptions;

namespace Donations.Api.Middlewares;

/// <summary>
/// Captura las excepciones de toda la petición y las convierte en una respuesta JSON
/// con el código HTTP adecuado: 400 (validación y reglas de negocio), 404 (no existe) o 500 (inesperado).
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // El mediador ejecuta los casos de uso por reflexión; si la excepción llega envuelta, se toma la original.
        if (exception is TargetInvocationException { InnerException: Exception innerException })
        {
            exception = innerException;
        }

        int statusCode;
        string error;
        string message;
        List<string> errors = [];

        switch (exception)
        {
            case CustomValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                error = "Validation";
                message = "Uno o más datos no son válidos.";
                errors = validationException.Errors;
                break;

            case BussinesRuleException:
                statusCode = StatusCodes.Status400BadRequest;
                error = "BusinessRule";
                message = exception.Message;
                break;

            case NotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                error = "NotFound";
                message = exception.Message;
                break;

            default:
                // Nunca se expone el mensaje de una excepción desconocida: el cliente no lo entiende
                // y revela parte de la estructura del sistema. Se registra en el log para el equipo.
                _logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
                statusCode = StatusCodes.Status500InternalServerError;
                error = "ServerError";
                message = "Ocurrió un error inesperado. Intente de nuevo más tarde.";
                break;
        }

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            error,
            message,
            errors
        });
    }
}