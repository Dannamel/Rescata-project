namespace Donations.Application.UseCases.Donations.Queries.GetDonationsList;

public sealed class DonationListItemDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal QuantityAmount { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public string FoodCategory { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableUntil { get; set; }
    public DateTime CreatedAt { get; set; }
}
