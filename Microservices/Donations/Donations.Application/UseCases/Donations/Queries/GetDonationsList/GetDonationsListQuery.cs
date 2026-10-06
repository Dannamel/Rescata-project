using Donations.Application.Utilities.Mediator;
using Donations.Application.Utilities.Pagination;

namespace Donations.Application.UseCases.Donations.Queries.GetDonationsList;

public sealed class GetDonationsListQuery : IRequest<PaginationResponse<DonationListItemDTO>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = PaginationRequest.DefaultPageSize;

    public string? Status { get; set; }

    public Guid? FoodCategoryId { get; set; }
}
