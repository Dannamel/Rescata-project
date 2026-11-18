using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Common.ValueObjects;
using Donations.Domain.Entities.Donations;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Application.UseCases.Donations.Commands.CreateDonation;

public sealed class CreateDonationUseCase : IRequestHandler<CreateDonationCommand, Guid>
{
    private readonly IDonationsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDonationUseCase(IDonationsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDonationCommand command)
    {
        bool foodCategoryExists = await _repository.FoodCategoryExistsAsync(command.FoodCategoryId);

        if (!foodCategoryExists)
        {
            throw new NotFoundException("La categoría del alimento no existe.");
        }

        Quantity quantity = new(command.QuantityAmount, command.QuantityUnit);

        PickupAddressInput addressInput = command.PickupAddress;
        Address pickupAddress = new(addressInput.RoadType,
                                    addressInput.RoadNumber,
                                    addressInput.CrossRoadNumber,
                                    addressInput.PlateNumber,
                                    addressInput.RoadSuffix,
                                    addressInput.City);

        Donation donation = new(command.BusinessId,
                                command.Title,
                                command.Description,
                                quantity,
                                command.FoodCategoryId,
                                pickupAddress,
                                command.AvailableUntil);

        await _repository.CreateAsync(donation);
        await _unitOfWork.CommitAsync();

        return donation.Id;
    }
}