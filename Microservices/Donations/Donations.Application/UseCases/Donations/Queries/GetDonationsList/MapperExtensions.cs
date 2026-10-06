using Donations.Application.Utilities.Pagination;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.Donations.Queries.GetDonationsList;

public static class MapperExtensions
{
    public static DonationListItemDTO ToListItemDTO(this Donation donation) => new()
    {
        Id = donation.Id,
        Title = donation.Title,
        QuantityAmount = donation.Quantity.Amount,
        QuantityUnit = donation.Quantity.Unit.ToString(),
        FoodCategory = donation.FoodCategory?.Name ?? string.Empty,
        Status = donation.Status.ToString(),
        AvailableUntil = donation.AvailableUntil,
        CreatedAt = donation.CreatedAt
    };

    public static PaginationResponse<DonationListItemDTO> ToListItemDTO(
        this PaginationResponse<Donation> source)
    {
        List<DonationListItemDTO> items = source.Items
                                                .Select(donation => donation.ToListItemDTO())
                                                .ToList();

        return new PaginationResponse<DonationListItemDTO>(items,
                                                           source.TotalCount,
                                                           source.PageNumber,
                                                           source.PageSize);
    }
}
