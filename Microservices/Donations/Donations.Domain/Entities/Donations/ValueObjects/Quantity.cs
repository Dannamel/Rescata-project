using Donations.Domain.Exceptions;

namespace Donations.Domain.Entities.Donations.ValueObjects;

/// <summary>Cantidad donada: valor + unidad (R4).</summary>
public sealed record Quantity
{
    public decimal Amount { get; private set; }
    public QuantityUnit Unit { get; private set; }

    private Quantity() { }

    public Quantity(decimal amount, QuantityUnit unit)
    {
        ApplyAmountRules(amount);
        ApplyUnitRules(unit);

        Amount = amount;
        Unit = unit;
    }

    private static void ApplyAmountRules(decimal amount)
    {
        if (amount <= 0)
            throw new BussinesRuleException("La cantidad debe ser mayor a cero.");
    }

    private static void ApplyUnitRules(QuantityUnit unit)
    {
        if (!Enum.IsDefined(unit))
            throw new BussinesRuleException("La unidad de medida no es válida. Use Kilogramos, Unidades o Litros.");
    }
}
