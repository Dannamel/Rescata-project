using Donations.Application.Utilities.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;
using Donations.Application.UseCases.Donations.Queries.GetDonationsList;
using Donations.Application.UseCases.Donations.Queries.GetDonationById;
using Donations.Application.Utilities.Pagination;
using FluentValidation;

namespace Donations.Application;

public static class ApplicationServicesRegistry
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMediator, SimpleMediator>();

        services.AddScoped<IRequestHandler<CreateDonationCommand, Guid>, CreateDonationUseCase>();

        services.AddScoped<IRequestHandler<GetDonationsListQuery, PaginationResponse<DonationListItemDTO>>, GetDonationsListUseCase>();
        services.AddScoped<IRequestHandler<GetDonationByIdQuery, DonationDetailDTO>, GetDonationByIdUseCase>();

        services.AddValidatorsFromAssemblyContaining<CreateDonationCommandValidator>();

        return services;
    }
}
