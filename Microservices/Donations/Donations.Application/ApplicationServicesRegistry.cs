using Donations.Application.Utilities.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Donations.Application;

public static class ApplicationServicesRegistry
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMediator, SimpleMediator>();

        // Registro de use cases: cada integrante agrega aquí sus handlers, al final.

        return services;
    }
}
