using Donations.Application.Utilities.Pagination;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.Contracts.Repositories;

public interface IDonationsRepository : IRepository<Donation>
{
    Task<PaginationResponse<Donation>> GetPagedListAsync(
        PaginationRequest pagination,
        DonationStatus? status,
        Guid? foodCategoryId);

    Task<bool> FoodCategoryExistsAsync(Guid foodCategoryId);

    Task<Donation?> GetByIdWithCategoryAsync(Guid id);
}
