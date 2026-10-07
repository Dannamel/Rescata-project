using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Application.UseCases.Donations.Commands.UpdateDonation;

public sealed class UpdateDonationCommand : IRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal QuantityAmount { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public DateTime AvailableUntil { get; set; }
}
