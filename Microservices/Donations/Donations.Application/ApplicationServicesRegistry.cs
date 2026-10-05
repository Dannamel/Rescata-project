using Donations.Application.Utilities.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;

namespace Donations.Application;

public static class ApplicationServicesRegistry
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMediator, SimpleMediator>();

        // Registro de use cases: cada integrante agrega aquí sus handlers, al final.
        services.AddScoped<IRequestHandler<CreateDonationCommand, Guid>, CreateDonationUseCase>();

        return services;
    }
}
