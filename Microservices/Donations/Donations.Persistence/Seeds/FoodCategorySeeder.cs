using Microsoft.EntityFrameworkCore;
using Donations.Domain.Entities.Donations;

namespace Donations.Persistence.Seeds
{
    public class FoodCategorySeeder : IDataSeeder
    {
        private readonly DataContext _context;

        public FoodCategorySeeder(DataContext context)
        {
            _context = context;
        }

        public int Order => 1;

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (await _context.FoodCategories.AnyAsync(cancellationToken))
            {
                return;
            }

            List<FoodCategory> categories =
            [
                new FoodCategory("Frutas y verduras"),
                new FoodCategory("Panadería"),
                new FoodCategory("Lácteos"),
                new FoodCategory("Comidas preparadas"),
                new FoodCategory("Enlatados y secos"),
                new FoodCategory("Carnes y proteínas"),
            ];

            await _context.FoodCategories.AddRangeAsync(categories, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
