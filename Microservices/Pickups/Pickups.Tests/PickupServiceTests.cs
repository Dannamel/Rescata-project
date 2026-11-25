using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Pickups.Api.Core;
using Pickups.Api.Data;
using Pickups.Api.Data.Entities;
using Pickups.Api.DTOs;
using Pickups.Api.Services.Pickups;
using Pickups.Tests.Fakes;

namespace Pickups.Tests;

/// <summary>
/// Sincronización entre Pickups y Donations. Cada operación usa un DataContext nuevo,
/// como cada petición HTTP en la API; todos comparten la misma base en memoria.
/// </summary>
[TestClass]
public sealed class PickupServiceTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid FoundationId = Guid.NewGuid();

    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = Guid.NewGuid().ToString();

    private DbContextOptions<DataContext> Options =>
        new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .Options;

    // ----- Flujo normal -----

    [TestMethod]
    public async Task NegocioConfirmaYFundacionCancela_RecogidaCancelledYDonacionAvailable()
    {
        FakeDonationsClient donations = new(BusinessId, "Available");
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Pending);

        donations.CallerRole = "Business";
        Result<PickupDTO> confirmed = await ConfirmAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(confirmed);
        Assert.AreEqual(PickupStatusEnum.Confirmed, confirmed.Value.Status);
        Assert.AreEqual("Claimed", donations.Status);

        donations.CallerRole = "Foundation";
        Result<PickupDTO> cancelled = await CancelAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(cancelled);
        Assert.AreEqual(PickupStatusEnum.Cancelled, cancelled.Value.Status);
        Assert.AreEqual(PickupStatusEnum.Cancelled, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("Available", donations.Status);
    }

    // ----- Donations cambia de estado y luego falla el guardado en Pickups -----

    [TestMethod]
    public async Task Completar_FallaElGuardado_ElReintentoDejaAmbosServiciosSincronizados()
    {
        FakeDonationsClient donations = new(BusinessId, "Claimed") { CallerRole = "Business" };
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Confirmed);

        Result<PickupDTO> firstAttempt = await CompleteAsync(new FailingDataContext(Options), donations, pickup.Id);

        // Estado intermedio: Donations ya marcó la donación, Pickups no alcanzó a guardar
        Assert.IsTrue(firstAttempt.IsFailure);
        Assert.AreEqual(ErrorType.Conflict, firstAttempt.Error.Type);
        Assert.AreEqual("PickedUp", donations.Status);
        Assert.AreEqual(PickupStatusEnum.Confirmed, await GetStoredStatusAsync(pickup.Id));

        Result<PickupDTO> retry = await CompleteAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(retry);
        Assert.AreEqual(PickupStatusEnum.Completed, retry.Value.Status);
        Assert.AreEqual(PickupStatusEnum.Completed, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("PickedUp", donations.Status);
        Assert.AreEqual(1, donations.CallsTo("picked-up"));
    }

    [TestMethod]
    public async Task CancelarConfirmadaComoFundacion_FallaElGuardado_ElReintentoDejaAmbosServiciosSincronizados()
    {
        FakeDonationsClient donations = new(BusinessId, "Claimed") { CallerRole = "Foundation" };
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Confirmed);

        Result<PickupDTO> firstAttempt = await CancelAsync(new FailingDataContext(Options), donations, pickup.Id);

        // Estado intermedio: Donations ya liberó la donación, Pickups no alcanzó a guardar
        Assert.IsTrue(firstAttempt.IsFailure);
        Assert.AreEqual(ErrorType.Conflict, firstAttempt.Error.Type);
        Assert.AreEqual("Available", donations.Status);
        Assert.AreEqual(PickupStatusEnum.Confirmed, await GetStoredStatusAsync(pickup.Id));

        Result<PickupDTO> retry = await CancelAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(retry);
        Assert.AreEqual(PickupStatusEnum.Cancelled, retry.Value.Status);
        Assert.AreEqual(PickupStatusEnum.Cancelled, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("Available", donations.Status);
        Assert.AreEqual(1, donations.CallsTo("release"));
        // Pickups ya no intenta volver a reservar con el token de la fundación (antes daba 403)
        Assert.AreEqual(0, donations.CallsTo("claim"));
    }

    [TestMethod]
    public async Task Confirmar_FallaElGuardado_ElReintentoDejaAmbosServiciosSincronizados()
    {
        FakeDonationsClient donations = new(BusinessId, "Available") { CallerRole = "Business" };
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Pending);

        Result<PickupDTO> firstAttempt = await ConfirmAsync(new FailingDataContext(Options), donations, pickup.Id);

        Assert.IsTrue(firstAttempt.IsFailure);
        Assert.AreEqual("Claimed", donations.Status);
        Assert.AreEqual(PickupStatusEnum.Pending, await GetStoredStatusAsync(pickup.Id));

        Result<PickupDTO> retry = await ConfirmAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(retry);
        Assert.AreEqual(PickupStatusEnum.Confirmed, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("Claimed", donations.Status);
        Assert.AreEqual(1, donations.CallsTo("claim"));
    }

    [TestMethod]
    public async Task Confirmar_FallaElGuardado_SiLaFundacionCancelaEnVezDeReintentar_LiberaLaDonacion()
    {
        FakeDonationsClient donations = new(BusinessId, "Available") { CallerRole = "Business" };
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Pending);

        await ConfirmAsync(new FailingDataContext(Options), donations, pickup.Id);

        donations.CallerRole = "Foundation";
        Result<PickupDTO> cancelled = await CancelAsync(NewContext(), donations, pickup.Id);

        AssertSuccess(cancelled);
        Assert.AreEqual(PickupStatusEnum.Cancelled, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("Available", donations.Status);
    }

    [TestMethod]
    public async Task Confirmar_OtraOperacionCancelaAlMismoTiempo_LiberaLaReserva()
    {
        FakeDonationsClient donations = new(BusinessId, "Available") { CallerRole = "Business" };
        Pickup pickup = await SeedPickupAsync(donations.DonationId, PickupStatusEnum.Pending);

        // Mientras el negocio confirma, la fundación cancela y guarda primero (cuando la donación aún no estaba reservada)
        FailingDataContext context = new(Options, beforeFailure: () => StoreStatusAsync(pickup.Id, PickupStatusEnum.Cancelled));

        Result<PickupDTO> confirmed = await ConfirmAsync(context, donations, pickup.Id);

        Assert.IsTrue(confirmed.IsFailure);
        Assert.AreEqual(ErrorType.Conflict, confirmed.Error.Type);
        Assert.AreEqual(PickupStatusEnum.Cancelled, await GetStoredStatusAsync(pickup.Id));
        Assert.AreEqual("Available", donations.Status);
    }

    // ----- Ayudas -----

    private DataContext NewContext() => new(Options);

    private static async Task<Result<PickupDTO>> ConfirmAsync(DataContext context, FakeDonationsClient donations, Guid pickupId)
    {
        await using (context)
        {
            return await new PickupService(context, donations).ConfirmAsync(pickupId, BusinessId, isAdmin: false);
        }
    }

    private static async Task<Result<PickupDTO>> CompleteAsync(DataContext context, FakeDonationsClient donations, Guid pickupId)
    {
        await using (context)
        {
            return await new PickupService(context, donations).CompleteAsync(pickupId, BusinessId, isAdmin: false);
        }
    }

    private static async Task<Result<PickupDTO>> CancelAsync(DataContext context, FakeDonationsClient donations, Guid pickupId)
    {
        await using (context)
        {
            CancelPickupDTO dto = new() { CancelReason = "La fundación no puede ir" };

            return await new PickupService(context, donations).CancelAsync(pickupId, FoundationId, dto, isAdmin: false);
        }
    }

    private async Task<Pickup> SeedPickupAsync(Guid donationId, PickupStatusEnum status)
    {
        Pickup pickup = new()
        {
            Id = Guid.NewGuid(),
            DonationId = donationId,
            UserId = FoundationId,
            BusinessId = BusinessId,
            ScheduledAt = DateTime.UtcNow.AddHours(2),
            DurationInMinutes = 30,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await using DataContext context = NewContext();
        context.Pickups.Add(pickup);
        await context.SaveChangesAsync();

        return pickup;
    }

    private async Task<PickupStatusEnum> GetStoredStatusAsync(Guid pickupId)
    {
        await using DataContext context = NewContext();
        Pickup pickup = await context.Pickups.AsNoTracking().SingleAsync(p => p.Id == pickupId);

        return pickup.Status;
    }

    private async Task StoreStatusAsync(Guid pickupId, PickupStatusEnum status)
    {
        await using DataContext context = NewContext();
        Pickup pickup = await context.Pickups.SingleAsync(p => p.Id == pickupId);
        pickup.Status = status;
        await context.SaveChangesAsync();
    }

    private static void AssertSuccess(Result result) =>
        Assert.IsTrue(result.IsSuccess, result.IsFailure ? $"{result.Error.Code}: {result.Error.Description}" : string.Empty);
}
