using System;
using System.Collections.Generic;
using System.Text;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Common.ValueObjects;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Application.UseCases.Donations.Commands.CreateDonation;

public sealed class CreateDonationCommand : IRequest<Guid>
{
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal QuantityAmount { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public Guid FoodCategoryId { get; set; }
    public PickupAddressInput PickupAddress { get; set; } = new();
    public DateTime AvailableUntil { get; set; }
}

public sealed class PickupAddressInput
{
    public RoadTypeEnum RoadType { get; set; }
    public string RoadNumber { get; set; } = string.Empty;
    public string CrossRoadNumber { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public RoadSuffixEnum RoadSuffix { get; set; }
    public string City { get; set; } = string.Empty;
}
