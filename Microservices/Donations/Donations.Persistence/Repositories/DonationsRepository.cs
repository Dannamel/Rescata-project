using Microsoft.EntityFrameworkCore;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Utilities.Pagination;
using Donations.Domain.Entities.Donations;
using Donations.Persistence.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.Repositories
{
    public class DonationsRepository : Repository<Donation>, IDonationsRepository
    {
        private readonly DataContext _context;

        public DonationsRepository(DataContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PaginationResponse<Donation>> GetPagedListAsync(PaginationRequest pagination,
                                                                          DonationStatus? status,
                                                                          Guid? foodCategoryId)
        {
            IQueryable<Donation> query = _context.Set<Donation>()
                                                 .Include(d => d.FoodCategory)
                                                 .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status);
            }

            if (foodCategoryId.HasValue)
            {
                query = query.Where(d => d.FoodCategoryId == foodCategoryId);
            }

            query = query.OrderByDescending(d => d.CreatedAt);

            return await query.ToPagedListAsync(pagination, CancellationToken.None);
        }

        public async Task<bool> FoodCategoryExistsAsync(Guid foodCategoryId)
        {
            return await _context.FoodCategories.AnyAsync(c => c.Id == foodCategoryId);
        }

        public async Task<Donation?> GetByIdWithCategoryAsync(Guid id)
        {
            return await _context.Set<Donation>()
                                 .Include(d => d.FoodCategory)
                                 .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
