using System.ComponentModel.DataAnnotations;

namespace Pickups.Api.DTOs;

public sealed class UpdatePickupDTO : IValidatableObject
{
    public DateTime ScheduledAt { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La duración debe ser mayor a cero.")]
    public int DurationInMinutes { get; set; }

    [MaxLength(512, ErrorMessage = "Las notas no pueden superar los 512 caracteres.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ScheduledAt == default)
        {
            yield return new ValidationResult(
                "La fecha de recogida es obligatoria.", [nameof(ScheduledAt)]);
        }
    }
}
