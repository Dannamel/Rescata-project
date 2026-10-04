using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Donations.Domain.Entities.Donations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.Configurations
{
    internal class DonationConfig : IEntityTypeConfiguration<Donation>
    {
        public void Configure(EntityTypeBuilder<Donation> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.BusinessId)
                   .IsRequired();

            builder.Property(d => d.Title)
                   .HasMaxLength(Donation.TitleMaxLength)
                   .IsRequired();

            builder.Property(d => d.Description)
                   .HasMaxLength(Donation.DescriptionMaxLength)
                   .IsRequired();

            builder.Property(d => d.AvailableUntil)
                   .IsRequired();

            builder.Property(d => d.Status)
                   .HasConversion<string>()
                   .HasMaxLength(16)
                   .IsRequired();

            builder.Property(d => d.CreatedAt)
                   .IsRequired();

            builder.OwnsOne(d => d.Quantity, quantity =>
            {
                quantity.Property(q => q.Amount)
                        .HasPrecision(18, 2)
                        .IsRequired();

                quantity.Property(q => q.Unit)
                        .HasConversion<string>()
                        .HasMaxLength(16)
                        .IsRequired();
            });

            builder.OwnsOne(d => d.PickupAddress, address =>
            {
                address.Property(a => a.RoadType).IsRequired();
                address.Property(a => a.RoadNumber).IsRequired()
                                                   .HasMaxLength(16);
                address.Property(a => a.CrossRoadNumber).IsRequired();
                address.Property(a => a.PlateNumber).IsRequired();
                address.Property(a => a.RoadSuffix).IsRequired();
                address.Property(a => a.City).IsRequired();
            });

            builder.HasOne(d => d.FoodCategory)
                   .WithMany()
                   .HasForeignKey(d => d.FoodCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
