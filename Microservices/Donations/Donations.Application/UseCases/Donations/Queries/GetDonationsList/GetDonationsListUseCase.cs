using Donations.Application.Contracts.Repositories;
using Donations.Application.Utilities.Mediator;
using Donations.Application.Utilities.Pagination;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.Donations.Queries.GetDonationsList;


public sealed class GetDonationsListUseCase
    : IRequestHandler<GetDonationsListQuery, PaginationResponse<DonationListItemDTO>>
{
    private readonly IDonationsRepository _repository;

    public GetDonationsListUseCase(IDonationsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginationResponse<DonationListItemDTO>> Handle(GetDonationsListQuery query)
    {
        PaginationRequest pagination = new(query.PageNumber, query.PageSize);

        DonationStatus? status = ParseStatus(query.Status);

        PaginationResponse<Donation> donations = await _repository.GetPagedListAsync(
            pagination,
            status,
            query.FoodCategoryId);

        return donations.ToListItemDTO();
    }

    private static DonationStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse(status, ignoreCase: true, out DonationStatus parsed) && Enum.IsDefined(parsed)
            ? parsed
            : null;
    }
}
