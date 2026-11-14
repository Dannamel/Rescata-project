using System.ComponentModel.DataAnnotations;

namespace Pickups.Api.Data.Entities;

/// <summary>Recogida programada por una fundación para una donación.</summary>
public class Pickup
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Donación del microservicio Donations que se va a recoger.</summary>
    public Guid DonationId { get; set; }

    /// <summary>Usuario de Identity (la fundación) que programó la recogida; sale del JWT.</summary>
    public Guid UserId { get; set; }

    public DateTime ScheduledAt { get; set; }

    public int DurationInMinutes { get; set; } = 30;

    public PickupStatusEnum Status { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(512)]
    public string? CancelReason { get; set; }
}
