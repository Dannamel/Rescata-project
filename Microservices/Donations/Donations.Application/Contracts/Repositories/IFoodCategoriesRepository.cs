using Donations.Domain.Entities.Donations;

namespace Donations.Application.Contracts.Repositories;

public interface IFoodCategoriesRepository : IRepository<FoodCategory>
{
    Task<IReadOnlyList<FoodCategory>> GetAllAsync();
}
