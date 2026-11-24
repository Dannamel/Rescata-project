using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.FoodCategories.Queries.GetFoodCategoriesList;

public sealed class GetFoodCategoriesListQuery : IRequest<IReadOnlyList<FoodCategoryDTO>>
{
}
