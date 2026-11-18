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

        // Una donación solo puede tener una recogida activa (Pending o Confirmed).
        // El índice único filtrado lo garantiza la base de datos, aunque lleguen dos solicitudes a la vez.
        modelBuilder.Entity<Pickup>()
            .HasIndex(p => p.DonationId)
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)PickupStatusEnum.Pending}, {(int)PickupStatusEnum.Confirmed})")
            .HasDatabaseName("UX_Pickups_ActiveDonation");

        base.OnModelCreating(modelBuilder);
    }
}
