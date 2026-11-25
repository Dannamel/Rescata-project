using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.Donations.Commands.ClaimDonation;

public sealed record ClaimDonationCommand(Guid Id, Guid? CurrentBusinessId = null) : IRequest;
