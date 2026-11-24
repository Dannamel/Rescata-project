using Microsoft.EntityFrameworkCore;
using Donations.Application.Contracts.Repositories;
using Donations.Domain.Entities.Donations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.Repositories
{
    public class FoodCategoriesRepository : Repository<FoodCategory>, IFoodCategoriesRepository
    {
        private readonly DataContext _context;

        public FoodCategoriesRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<FoodCategory>> GetAllAsync()
        {
            return await _context.FoodCategories
                                 .AsNoTracking()
                                 .OrderBy(c => c.Name)
                                 .ToListAsync();
        }
    }
}
