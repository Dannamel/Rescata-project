using Donations.Domain.Entities.Donations;

namespace Donations.Application.UseCases.FoodCategories.Queries.GetFoodCategoriesList;

public static class MapperExtensions
{
    public static FoodCategoryDTO ToDTO(this FoodCategory category) => new()
    {
        Id = category.Id,
        Name = category.Name
    };

    public static IReadOnlyList<FoodCategoryDTO> ToDTO(this IReadOnlyList<FoodCategory> categories)
    {
        return categories.Select(category => category.ToDTO())
                         .ToList();
    }
}
