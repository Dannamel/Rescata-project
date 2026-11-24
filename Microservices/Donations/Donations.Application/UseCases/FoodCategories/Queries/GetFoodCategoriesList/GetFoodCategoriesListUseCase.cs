using Donations.Application.Contracts.Repositories;
using Donations.Application.Utilities.Mediator;
using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.FoodCategories.Queries.GetFoodCategoriesList;


public sealed class GetFoodCategoriesListUseCase
    : IRequestHandler<GetFoodCategoriesListQuery, IReadOnlyList<FoodCategoryDTO>>
{
    private readonly IFoodCategoriesRepository _repository;

    public GetFoodCategoriesListUseCase(IFoodCategoriesRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FoodCategoryDTO>> Handle(GetFoodCategoriesListQuery query)
    {
        IReadOnlyList<FoodCategory> categories = await _repository.GetAllAsync();

        return categories.ToDTO();
    }
}
