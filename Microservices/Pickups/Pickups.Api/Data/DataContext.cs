using Microsoft.EntityFrameworkCore;
using Pickups.Api.Data.Entities;

namespace Pickups.Api.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Pickup> Pickups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Para buscar rápido las recogidas de una donación y las de un usuario, ordenadas por fecha
        modelBuilder.Entity<Pickup>().HasIndex(p => new { p.DonationId, p.ScheduledAt });
        modelBuilder.Entity<Pickup>().HasIndex(p => new { p.UserId, p.ScheduledAt });

        base.OnModelCreating(modelBuilder);
    }
}
