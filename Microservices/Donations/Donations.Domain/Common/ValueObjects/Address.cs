using Donations.Domain.Exceptions;

namespace Donations.Domain.Common.ValueObjects;

/// <summary>
/// Dirección con nomenclatura colombiana. Ej: Calle 10 # 43A - 25 Sur, Medellín.
/// </summary>
public sealed record Address
{
    public RoadTypeEnum RoadType { get; private set; }
    public string RoadNumber { get; private set; } = null!;
    public string CrossRoadNumber { get; private set; } = null!;
    public string PlateNumber { get; private set; } = null!;
    public RoadSuffixEnum RoadSuffix { get; private set; }
    public string City { get; private set; } = null!;

    private Address() { }

    public Address(RoadTypeEnum roadType, string roadNumber, string crossRoadNumber, string plateNumber,
        RoadSuffixEnum roadSuffix, string city)
    {
        if (!Enum.IsDefined(roadType))
            throw new BussinesRuleException("El tipo de vía no es válido.");
        if (!Enum.IsDefined(roadSuffix))
            throw new BussinesRuleException("El sufijo de la vía no es válido.");

        RoadType = roadType;
        RoadNumber = Required(roadNumber, "El número de la vía es obligatorio.");
        CrossRoadNumber = Required(crossRoadNumber, "El número de la vía que cruza es obligatorio.");
        PlateNumber = Required(plateNumber, "El número de placa es obligatorio.");
        RoadSuffix = roadSuffix;
        City = Required(city, "La ciudad es obligatoria.");
    }

    private static string Required(string value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new BussinesRuleException(message) : value.Trim();

    public override string ToString()
    {
        var suffix = RoadSuffix == RoadSuffixEnum.Ninguno ? string.Empty : $" {RoadSuffix}";
        return $"{RoadType} {RoadNumber} # {CrossRoadNumber} - {PlateNumber}{suffix}, {City}";
    }
}
