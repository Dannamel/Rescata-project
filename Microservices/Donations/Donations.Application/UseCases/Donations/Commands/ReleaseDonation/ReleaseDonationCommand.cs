using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.Donations.Commands.ReleaseDonation;

public sealed record ReleaseDonationCommand(Guid Id, Guid? CurrentBusinessId = null) : IRequest;
