using Donations.Application.Utilities.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Donations.Application.UseCases.Donations.Commands.CancelDonation;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;
using Donations.Application.UseCases.Donations.Commands.UpdateDonation;
using FluentValidation;

namespace Donations.Application;

public static class ApplicationServicesRegistry
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMediator, SimpleMediator>();

        // Registro de use cases: cada integrante agrega aquí sus handlers, al final.
        services.AddScoped<IRequestHandler<CreateDonationCommand, Guid>, CreateDonationUseCase>();
        services.AddScoped<IRequestHandler<UpdateDonationCommand>, UpdateDonationUseCase>();
        services.AddScoped<IRequestHandler<CancelDonationCommand>, CancelDonationUseCase>();
        // Validations: registra todos los validadores de FluentValidation de este proyecto.
        services.AddValidatorsFromAssemblyContaining<CreateDonationCommandValidator>();

        return services;
    }
}
