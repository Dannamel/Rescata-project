using System.ComponentModel.DataAnnotations;

namespace Pickups.Api.DTOs;

public sealed class CreatePickupDTO : IValidatableObject
{
    public Guid DonationId { get; set; }

    public DateTime ScheduledAt { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La duración debe ser mayor a cero.")]
    public int DurationInMinutes { get; set; } = 30;

    [MaxLength(512, ErrorMessage = "Las notas no pueden superar los 512 caracteres.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DonationId == Guid.Empty)
        {
            yield return new ValidationResult(
                "El identificador de la donación es obligatorio.", [nameof(DonationId)]);
        }

        if (ScheduledAt == default)
        {
            yield return new ValidationResult(
                "La fecha de recogida es obligatoria.", [nameof(ScheduledAt)]);
        }
    }
}
