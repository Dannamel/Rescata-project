using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Application.UseCases.Donations.Commands.UpdateDonation;

public sealed class UpdateDonationUseCase : IRequestHandler<UpdateDonationCommand>
{
    private readonly IDonationsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDonationUseCase(IDonationsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDonationCommand command)
    {
        Donation? donation = await _repository.GetByIdAsync(command.Id);

        if (donation is null)
        {
            throw new NotFoundException("La donación no existe.");
        }

        Quantity quantity = new(command.QuantityAmount, command.QuantityUnit);

        donation.UpdateTitle(command.Title);
        donation.UpdateDescription(command.Description);
        donation.UpdateQuantity(quantity);
        DateTime availableUntilUtc = command.AvailableUntil.Kind switch
        {
            DateTimeKind.Utc => command.AvailableUntil,
            DateTimeKind.Local => command.AvailableUntil.ToUniversalTime(),
            _ => DateTime.SpecifyKind(command.AvailableUntil, DateTimeKind.Utc)
        };

        if (availableUntilUtc != donation.AvailableUntil)
        {
            donation.ExtendDeadline(availableUntilUtc);
        }

        await _repository.UpdateAsync(donation);
        await _unitOfWork.CommitAsync();
    }
}
