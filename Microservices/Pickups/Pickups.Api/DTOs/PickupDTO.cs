using Pickups.Api.Data.Entities;

namespace Pickups.Api.DTOs;

public sealed class PickupDTO
{
    public Guid Id { get; set; }
    public Guid DonationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationInMinutes { get; set; }
    public PickupStatusEnum Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
