using System.ComponentModel.DataAnnotations;

namespace Pickups.Api.DTOs;

public sealed class CancelPickupDTO
{
    [MaxLength(512, ErrorMessage = "El motivo de cancelación no puede superar los 512 caracteres.")]
    public string? CancelReason { get; set; }
}
