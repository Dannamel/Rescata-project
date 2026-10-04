using Donations.Domain.Common.ValueObjects;
using Donations.Domain.Entities.Donations.ValueObjects;
using Donations.Domain.Exceptions;

namespace Donations.Domain.Entities.Donations;

/// <summary>Agregado raíz: excedente de comida que un negocio publica para ser rescatado.</summary>
public sealed class Donation
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public static readonly TimeSpan MinAvailability = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaxAvailability = TimeSpan.FromDays(7);

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public Guid FoodCategoryId { get; private set; }
    public FoodCategory FoodCategory { get; private set; } = null!;
    public Address PickupAddress { get; private set; } = null!;
    public DateTime AvailableUntil { get; private set; }
    public DonationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Donation() { }

    public Donation(Guid businessId, string title, string description, Quantity quantity,
        Guid foodCategoryId, Address pickupAddress, DateTime availableUntil)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(pickupAddress);

        ApplyBusinessIdRules(businessId);
        ApplyTitleRules(title);
        ApplyDescriptionRules(description);
        ApplyFoodCategoryRules(foodCategoryId);
        var availableUntilUtc = ApplyAvailableUntilRules(availableUntil);

        Id = Guid.CreateVersion7();
        BusinessId = businessId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Quantity = quantity;
        FoodCategoryId = foodCategoryId;
        PickupAddress = pickupAddress;
        AvailableUntil = availableUntilUtc;
        Status = DonationStatus.Available;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        ApplyEditableRules();
        ApplyTitleRules(title);
        Title = title.Trim();
    }

    public void UpdateDescription(string description)
    {
        ApplyEditableRules();
        ApplyDescriptionRules(description);
        Description = description?.Trim() ?? string.Empty;
    }

    public void UpdateQuantity(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);
        ApplyEditableRules();
        Quantity = quantity;
    }

    public void ExtendDeadline(DateTime newAvailableUntil)
    {
        ApplyEditableRules();

        var newAvailableUntilUtc = ToUtc(newAvailableUntil);
        if (newAvailableUntilUtc <= AvailableUntil)
            throw new BussinesRuleException("La nueva hora límite debe ser posterior a la actual.");

        AvailableUntil = ApplyAvailableUntilRules(newAvailableUntilUtc);
    }

    public void Cancel()
    {
        if (Status != DonationStatus.Available)
            throw new BussinesRuleException("No se puede cancelar una donación que ya fue reclamada o recogida.");

        Status = DonationStatus.Cancelled;
    }

    // R1
    private static void ApplyBusinessIdRules(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new BussinesRuleException("El negocio que publica la donación es obligatorio.");
    }

    // R2
    private static void ApplyTitleRules(string title)
    {
        var length = title?.Trim().Length ?? 0;
        if (length < TitleMinLength || length > TitleMaxLength)
            throw new BussinesRuleException(
                $"El título debe tener entre {TitleMinLength} y {TitleMaxLength} caracteres.");
    }

    // R3
    private static void ApplyDescriptionRules(string description)
    {
        if ((description?.Trim().Length ?? 0) > DescriptionMaxLength)
            throw new BussinesRuleException(
                $"La descripción no puede superar {DescriptionMaxLength} caracteres.");
    }

    // R5
    private static DateTime ApplyAvailableUntilRules(DateTime availableUntil)
    {
        var availableUntilUtc = ToUtc(availableUntil);
        var now = DateTime.UtcNow;

        if (availableUntilUtc < now.Add(MinAvailability) || availableUntilUtc > now.Add(MaxAvailability))
            throw new BussinesRuleException("La hora límite debe estar entre 1 hora y 7 días a partir de ahora.");

        return availableUntilUtc;
    }

    // R7
    private void ApplyEditableRules()
    {
        if (Status != DonationStatus.Available)
            throw new BussinesRuleException("Solo se pueden modificar donaciones disponibles.");
    }

    // R9
    private static void ApplyFoodCategoryRules(Guid foodCategoryId)
    {
        if (foodCategoryId == Guid.Empty)
            throw new BussinesRuleException("La categoría del alimento es obligatoria.");
    }

    // Las fechas sin zona (Unspecified) se interpretan como UTC.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
