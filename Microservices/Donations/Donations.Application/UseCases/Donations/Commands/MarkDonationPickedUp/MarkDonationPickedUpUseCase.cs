using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.Donations.Commands.MarkDonationPickedUp;

public sealed class MarkDonationPickedUpUseCase : IRequestHandler<MarkDonationPickedUpCommand>
{
    private readonly IDonationsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkDonationPickedUpUseCase(IDonationsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkDonationPickedUpCommand command)
    {
        Donation? donation = await _repository.GetByIdAsync(command.Id);

        if (donation is null)
        {
            throw new NotFoundException("La donación no existe.");
        }

        if (command.CurrentBusinessId is not null && !donation.BelongsTo(command.CurrentBusinessId.Value))
        {
            throw new ForbiddenException("La donación pertenece a otro negocio.");
        }

        donation.MarkPickedUp();

        await _repository.UpdateAsync(donation);
        await _unitOfWork.CommitAsync();
    }
}
