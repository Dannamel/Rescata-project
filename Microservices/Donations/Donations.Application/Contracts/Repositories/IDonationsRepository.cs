using Donations.Application.Utilities.Pagination;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.Contracts.Repositories;

public interface IDonationsRepository : IRepository<Donation>
{
    /// <summary>Listado paginado con filtros opcionales por estado y categoría.</summary>
    Task<PaginationResponse<Donation>> GetPagedListAsync(
        PaginationRequest pagination,
        DonationStatus? status,
        Guid? foodCategoryId);
}
