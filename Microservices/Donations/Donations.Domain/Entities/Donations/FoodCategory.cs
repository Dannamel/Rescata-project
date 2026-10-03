namespace Donations.Domain.Entities.Donations;

/// <summary>
/// Categoría de alimento. Las categorías (Frutas y verduras, Panadería, Lácteos, Comidas preparadas,
/// Enlatados y secos, Carnes y proteínas) se cargan con el seeder de Persistence.
/// </summary>
public sealed class FoodCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    private FoodCategory() { }

    public FoodCategory(string name)
    {
        Id = Guid.CreateVersion7();
        Name = name;
    }
}
