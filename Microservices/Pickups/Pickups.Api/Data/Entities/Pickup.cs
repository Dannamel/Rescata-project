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

    /// <summary>
    /// Negocio dueño de la donación. Se copia de Donations al crear la recogida,
    /// para que el negocio pueda consultar las recogidas de sus donaciones sin llamar a Donations.
    /// </summary>
    public Guid BusinessId { get; set; }

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

    /// <summary>
    /// SQL Server la cambia en cada UPDATE. Si dos operaciones leen la misma versión y ambas guardan,
    /// la segunda falla con DbUpdateConcurrencyException (concurrencia optimista).
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
