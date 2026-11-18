using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.Donations.Queries.GetDonationById;


public sealed class GetDonationByIdUseCase
    : IRequestHandler<GetDonationByIdQuery, DonationDetailDTO>
{
    private readonly IDonationsRepository _repository;

    public GetDonationByIdUseCase(IDonationsRepository repository)
    {
        _repository = repository;
    }

    public async Task<DonationDetailDTO> Handle(GetDonationByIdQuery query)
    {
        Donation? donation = await _repository.GetByIdWithCategoryAsync(query.Id);

        if (donation is null)
        {
            throw new NotFoundException($"No existe una donación con el Id {query.Id}.");
        }

        return donation.ToDetailDTO();
    }
}

internal static class DonationDetailMapperExtensions
{
    public static DonationDetailDTO ToDetailDTO(this Donation donation) => new()
    {
        Id = donation.Id,
        BusinessId = donation.BusinessId,
        Title = donation.Title,
        Description = donation.Description,
        QuantityAmount = donation.Quantity.Amount,
        QuantityUnit = donation.Quantity.Unit.ToString(),
        FoodCategoryId = donation.FoodCategoryId,
        FoodCategory = donation.FoodCategory?.Name ?? string.Empty,
        PickupAddress = donation.PickupAddress.ToString(),
        Status = donation.Status.ToString(),
        AvailableUntil = donation.AvailableUntil,
        CreatedAt = donation.CreatedAt
    };
}
