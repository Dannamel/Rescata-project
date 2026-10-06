namespace Donations.Application.UseCases.Donations.Queries.GetDonationById;

public sealed class DonationDetailDTO
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal QuantityAmount { get; set; }
    public string QuantityUnit { get; set; } = string.Empty;
    public Guid FoodCategoryId { get; set; }
    public string FoodCategory { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableUntil { get; set; }
    public DateTime CreatedAt { get; set; }
}
