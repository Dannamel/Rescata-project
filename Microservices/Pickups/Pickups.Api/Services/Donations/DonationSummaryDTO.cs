namespace Pickups.Api.Services.Donations;


public sealed class DonationSummaryDTO
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime AvailableUntil { get; set; }

    public string PickupAddress { get; set; } = string.Empty;
}
