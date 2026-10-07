using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.Donations.Commands.CancelDonation;

public sealed record CancelDonationCommand(Guid Id) : IRequest;
