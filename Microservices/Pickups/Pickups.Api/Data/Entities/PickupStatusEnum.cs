namespace Pickups.Api.Data.Entities;

/// <summary>Estado de una recogida. Empieza en 1 para que el valor por defecto (0) no sea válido.</summary>
public enum PickupStatusEnum
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4
}
