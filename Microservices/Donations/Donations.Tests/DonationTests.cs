using Donations.Domain.Common.ValueObjects;
using Donations.Domain.Entities.Donations;
using Donations.Domain.Entities.Donations.ValueObjects;
using Donations.Domain.Exceptions;

namespace Donations.Tests;

[TestClass]
public sealed class DonationTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid FoodCategoryId = Guid.NewGuid();

    private static Quantity ValidQuantity() => new(10m, QuantityUnit.Kilogramos);

    private static Address ValidAddress() =>
        new(RoadTypeEnum.Calle, "10", "43A", "25", RoadSuffixEnum.Sur, "Medellín");

    private static DateTime ValidAvailableUntil() => DateTime.UtcNow.AddHours(5);

    private static Donation CreateDonation(
        Guid? businessId = null,
        string title = "Pan del día",
        string description = "Pan fresco horneado esta mañana.",
        Quantity? quantity = null,
        Guid? foodCategoryId = null,
        DateTime? availableUntil = null) =>
        new(businessId ?? BusinessId, title, description, quantity ?? ValidQuantity(),
            foodCategoryId ?? FoodCategoryId, ValidAddress(), availableUntil ?? ValidAvailableUntil());

    private static Donation CreateCancelledDonation()
    {
        var donation = CreateDonation();
        donation.Cancel();
        return donation;
    }

    // ----- Creación válida -----

    [TestMethod]
    public void Constructor_ConDatosValidos_CreaDonacionDisponible()
    {
        var before = DateTime.UtcNow;
        var availableUntil = ValidAvailableUntil();

        var donation = CreateDonation(availableUntil: availableUntil);

        Assert.AreNotEqual(Guid.Empty, donation.Id);
        Assert.AreEqual(7, donation.Id.Version);
        Assert.AreEqual(BusinessId, donation.BusinessId);
        Assert.AreEqual("Pan del día", donation.Title);
        Assert.AreEqual("Pan fresco horneado esta mañana.", donation.Description);
        Assert.AreEqual(ValidQuantity(), donation.Quantity);
        Assert.AreEqual(FoodCategoryId, donation.FoodCategoryId);
        Assert.AreEqual(ValidAddress(), donation.PickupAddress);
        Assert.AreEqual(availableUntil, donation.AvailableUntil);
        Assert.AreEqual(DonationStatus.Available, donation.Status);
        Assert.AreEqual(DateTimeKind.Utc, donation.CreatedAt.Kind);
        Assert.IsTrue(donation.CreatedAt >= before && donation.CreatedAt <= DateTime.UtcNow);
    }

    // ----- R1: BusinessId obligatorio -----

    [TestMethod]
    public void R1_Constructor_SinBusinessId_LanzaBussinesRuleException()
    {
        var ex = Assert.Throws<BussinesRuleException>(() => CreateDonation(businessId: Guid.Empty));
        Assert.AreEqual("El negocio que publica la donación es obligatorio.", ex.Message);
    }

    // ----- R2: Title entre 3 y 100 caracteres -----

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("Pa")]
    public void R2_Constructor_TituloMenorA3_LanzaBussinesRuleException(string title)
    {
        var ex = Assert.Throws<BussinesRuleException>(() => CreateDonation(title: title));
        Assert.AreEqual("El título debe tener entre 3 y 100 caracteres.", ex.Message);
    }

    [TestMethod]
    public void R2_Constructor_TituloMayorA100_LanzaBussinesRuleException()
    {
        Assert.Throws<BussinesRuleException>(() => CreateDonation(title: new string('a', 101)));
    }

    [TestMethod]
    public void R2_Constructor_TituloEnLimites_EsValido()
    {
        Assert.AreEqual("Pan", CreateDonation(title: "Pan").Title);
        Assert.AreEqual(100, CreateDonation(title: new string('a', 100)).Title.Length);
    }

    [TestMethod]
    public void R2_UpdateTitle_TituloInvalido_LanzaBussinesRuleException()
    {
        var donation = CreateDonation();

        Assert.Throws<BussinesRuleException>(() => donation.UpdateTitle("Pa"));
        Assert.Throws<BussinesRuleException>(() => donation.UpdateTitle(new string('a', 101)));
        Assert.AreEqual("Pan del día", donation.Title);
    }

    // ----- R3: Description máximo 500 caracteres -----

    [TestMethod]
    public void R3_Constructor_DescripcionMayorA500_LanzaBussinesRuleException()
    {
        var ex = Assert.Throws<BussinesRuleException>(() => CreateDonation(description: new string('a', 501)));
        Assert.AreEqual("La descripción no puede superar 500 caracteres.", ex.Message);
    }

    [TestMethod]
    public void R3_Constructor_DescripcionDe500_EsValida()
    {
        Assert.AreEqual(500, CreateDonation(description: new string('a', 500)).Description.Length);
    }

    [TestMethod]
    public void R3_UpdateDescription_DescripcionMayorA500_LanzaBussinesRuleException()
    {
        var donation = CreateDonation();

        Assert.Throws<BussinesRuleException>(() => donation.UpdateDescription(new string('a', 501)));
    }

    // ----- R4: Quantity mayor a 0 y unidad válida -----

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void R4_Quantity_CantidadNoPositiva_LanzaBussinesRuleException(int amount)
    {
        var ex = Assert.Throws<BussinesRuleException>(() => new Quantity(amount, QuantityUnit.Unidades));
        Assert.AreEqual("La cantidad debe ser mayor a cero.", ex.Message);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(99)]
    public void R4_Quantity_UnidadInvalida_LanzaBussinesRuleException(int unit)
    {
        Assert.Throws<BussinesRuleException>(() => new Quantity(5m, (QuantityUnit)unit));
    }

    [TestMethod]
    [DataRow(QuantityUnit.Kilogramos)]
    [DataRow(QuantityUnit.Unidades)]
    [DataRow(QuantityUnit.Litros)]
    public void R4_Quantity_DatosValidos_SeCrea(QuantityUnit unit)
    {
        var quantity = new Quantity(2.5m, unit);

        Assert.AreEqual(2.5m, quantity.Amount);
        Assert.AreEqual(unit, quantity.Unit);
    }

    // ----- R5: AvailableUntil entre 1 hora y 7 días -----

    [TestMethod]
    public void R5_Constructor_HoraLimiteEnElPasado_LanzaBussinesRuleException()
    {
        var ex = Assert.Throws<BussinesRuleException>(
            () => CreateDonation(availableUntil: DateTime.UtcNow.AddHours(-1)));
        Assert.AreEqual("La hora límite debe estar entre 1 hora y 7 días a partir de ahora.", ex.Message);
    }

    [TestMethod]
    public void R5_Constructor_HoraLimiteMenorA1Hora_LanzaBussinesRuleException()
    {
        Assert.Throws<BussinesRuleException>(
            () => CreateDonation(availableUntil: DateTime.UtcNow.AddMinutes(59)));
    }

    [TestMethod]
    public void R5_Constructor_HoraLimiteMayorA7Dias_LanzaBussinesRuleException()
    {
        Assert.Throws<BussinesRuleException>(
            () => CreateDonation(availableUntil: DateTime.UtcNow.AddDays(7).AddMinutes(1)));
    }

    [TestMethod]
    public void R5_Constructor_HoraLimiteDentroDelRango_EsValida()
    {
        CreateDonation(availableUntil: DateTime.UtcNow.AddMinutes(61));
        CreateDonation(availableUntil: DateTime.UtcNow.AddDays(7).AddMinutes(-1));
    }

    [TestMethod]
    public void R5_ExtendDeadline_MayorA7Dias_LanzaBussinesRuleException()
    {
        var donation = CreateDonation();

        Assert.Throws<BussinesRuleException>(() => donation.ExtendDeadline(DateTime.UtcNow.AddDays(8)));
    }

    // ----- R6: la nueva hora límite debe ser posterior a la actual -----

    [TestMethod]
    public void R6_ExtendDeadline_FechaAnterior_LanzaBussinesRuleException()
    {
        var donation = CreateDonation(availableUntil: DateTime.UtcNow.AddHours(5));

        var ex = Assert.Throws<BussinesRuleException>(() => donation.ExtendDeadline(DateTime.UtcNow.AddHours(3)));
        Assert.AreEqual("La nueva hora límite debe ser posterior a la actual.", ex.Message);
    }

    [TestMethod]
    public void R6_ExtendDeadline_MismaFecha_LanzaBussinesRuleException()
    {
        var donation = CreateDonation();

        Assert.Throws<BussinesRuleException>(() => donation.ExtendDeadline(donation.AvailableUntil));
    }

    // ----- R7: solo se edita si está Available -----

    [TestMethod]
    public void R7_UpdateTitle_DonacionNoDisponible_LanzaBussinesRuleException()
    {
        var donation = CreateCancelledDonation();

        var ex = Assert.Throws<BussinesRuleException>(() => donation.UpdateTitle("Nuevo título"));
        Assert.AreEqual("Solo se pueden modificar donaciones disponibles.", ex.Message);
    }

    [TestMethod]
    public void R7_UpdateDescription_DonacionNoDisponible_LanzaBussinesRuleException()
    {
        var donation = CreateCancelledDonation();

        Assert.Throws<BussinesRuleException>(() => donation.UpdateDescription("Nueva descripción"));
    }

    [TestMethod]
    public void R7_UpdateQuantity_DonacionNoDisponible_LanzaBussinesRuleException()
    {
        var donation = CreateCancelledDonation();

        Assert.Throws<BussinesRuleException>(() => donation.UpdateQuantity(new Quantity(3m, QuantityUnit.Litros)));
    }

    [TestMethod]
    public void R7_ExtendDeadline_DonacionNoDisponible_LanzaBussinesRuleException()
    {
        var donation = CreateCancelledDonation();

        var ex = Assert.Throws<BussinesRuleException>(() => donation.ExtendDeadline(DateTime.UtcNow.AddDays(2)));
        Assert.AreEqual("Solo se pueden modificar donaciones disponibles.", ex.Message);
    }

    // ----- R8: solo se cancela si está Available -----

    [TestMethod]
    public void R8_Cancel_DonacionYaCancelada_LanzaBussinesRuleException()
    {
        var donation = CreateCancelledDonation();

        var ex = Assert.Throws<BussinesRuleException>(donation.Cancel);
        Assert.AreEqual("No se puede cancelar una donación que ya fue reclamada o recogida.", ex.Message);
    }

    // ----- R9: categoría obligatoria -----

    [TestMethod]
    public void R9_Constructor_SinCategoria_LanzaBussinesRuleException()
    {
        var ex = Assert.Throws<BussinesRuleException>(() => CreateDonation(foodCategoryId: Guid.Empty));
        Assert.AreEqual("La categoría del alimento es obligatoria.", ex.Message);
    }

    // ----- Métodos de comportamiento válidos -----

    [TestMethod]
    public void UpdateTitle_TituloValido_ActualizaTitulo()
    {
        var donation = CreateDonation();

        donation.UpdateTitle("  Pan integral  ");

        Assert.AreEqual("Pan integral", donation.Title);
    }

    [TestMethod]
    public void UpdateDescription_DescripcionValida_ActualizaDescripcion()
    {
        var donation = CreateDonation();

        donation.UpdateDescription("Pan de ayer, en buen estado.");

        Assert.AreEqual("Pan de ayer, en buen estado.", donation.Description);
    }

    [TestMethod]
    public void UpdateQuantity_CantidadValida_ActualizaCantidad()
    {
        var donation = CreateDonation();
        var newQuantity = new Quantity(20m, QuantityUnit.Unidades);

        donation.UpdateQuantity(newQuantity);

        Assert.AreEqual(newQuantity, donation.Quantity);
    }

    [TestMethod]
    public void ExtendDeadline_FechaPosteriorDentroDelRango_ActualizaHoraLimite()
    {
        var donation = CreateDonation(availableUntil: DateTime.UtcNow.AddHours(5));
        var newDeadline = DateTime.UtcNow.AddDays(2);

        donation.ExtendDeadline(newDeadline);

        Assert.AreEqual(newDeadline, donation.AvailableUntil);
    }

    [TestMethod]
    public void Cancel_DonacionDisponible_CambiaEstadoACancelled()
    {
        var donation = CreateDonation();

        donation.Cancel();

        Assert.AreEqual(DonationStatus.Cancelled, donation.Status);
    }
}
