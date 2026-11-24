using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.Donations.Commands.ClaimDonation;

public sealed class ClaimDonationUseCase : IRequestHandler<ClaimDonationCommand>
{
    private readonly IDonationsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ClaimDonationUseCase(IDonationsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ClaimDonationCommand command)
    {
        Donation? donation = await _repository.GetByIdAsync(command.Id);

        if (donation is null)
        {
            throw new NotFoundException("La donación no existe.");
        }

        donation.Claim();

        await _repository.UpdateAsync(donation);
        await _unitOfWork.CommitAsync();
    }
}
