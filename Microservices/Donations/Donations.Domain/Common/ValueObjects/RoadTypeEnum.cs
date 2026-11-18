namespace Donations.Domain.Common.ValueObjects;

// Empieza en 1 para que el valor por defecto (0) no sea un tipo de vía válido.
public enum RoadTypeEnum
{
    Calle = 1,
    Carrera = 2,
    Avenida = 3,
    Diagonal = 4,
    Transversal = 5,
    Circular = 6
}
