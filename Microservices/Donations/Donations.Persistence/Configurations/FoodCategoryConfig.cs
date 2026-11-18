using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Donations.Domain.Entities.Donations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.Configurations
{
    internal class FoodCategoryConfig : IEntityTypeConfiguration<FoodCategory>
    {
        public void Configure(EntityTypeBuilder<FoodCategory> builder)
        {
            builder.HasKey(fc => fc.Id);

            builder.Property(fc => fc.Name)
                   .HasMaxLength(64)
                   .IsRequired();
        }
    }
}
