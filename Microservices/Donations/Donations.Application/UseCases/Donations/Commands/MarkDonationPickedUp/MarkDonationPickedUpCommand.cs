using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.Donations.Commands.MarkDonationPickedUp;

public sealed record MarkDonationPickedUpCommand(Guid Id) : IRequest;
