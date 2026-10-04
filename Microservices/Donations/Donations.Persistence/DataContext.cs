using Microsoft.EntityFrameworkCore;
using Donations.Domain.Entities.Donations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {            
        }

        public DbSet<Donation> Donations { get; set; }
        public DbSet<FoodCategory> FoodCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);

            base.OnModelCreating(builder);
        }
    }
}
