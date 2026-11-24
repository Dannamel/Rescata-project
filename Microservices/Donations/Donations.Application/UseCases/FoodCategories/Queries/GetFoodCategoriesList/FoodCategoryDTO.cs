namespace Donations.Application.UseCases.FoodCategories.Queries.GetFoodCategoriesList;

public sealed class FoodCategoryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
