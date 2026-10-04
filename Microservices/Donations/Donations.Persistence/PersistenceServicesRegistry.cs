using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Persistence.Repositories;
using Donations.Persistence.Seeds;
using Donations.Persistence.UnitOfWorks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence
{
    public static class PersistenceServicesRegistry
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("MyConnection"));
            });

            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
            services.AddScoped<IDonationsRepository, DonationsRepository>();

            // Seeders
            services.AddScoped<IDataSeeder, FoodCategorySeeder>();

            return services;
        }
    }
}
