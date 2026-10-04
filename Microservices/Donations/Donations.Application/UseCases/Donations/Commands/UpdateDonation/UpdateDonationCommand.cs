using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Application.UseCases.Donations.Commands.UpdateDonation;

public sealed record UpdateDonationCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Quantity,
    QuantityUnit Unit,
    DateTime AvailableUntil) : IRequest;
