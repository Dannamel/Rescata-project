using Pickups.Api.Core;
using Pickups.Api.Services.Donations;

namespace Pickups.Tests.Fakes;

/// <summary>
/// Simula el microservicio Donations en memoria, con las mismas reglas que su dominio y su controlador:
/// reservar (claim) solo desde Available; liberar (release) y marcar como recogida (picked-up) solo desde Claimed.
/// Reservar y marcar como recogida exigen Business o Admin; liberar también lo puede hacer Foundation.
/// </summary>
public sealed class FakeDonationsClient : IDonationsClient
{
    private readonly List<string> _calls = [];

    public FakeDonationsClient(Guid businessId, string status)
    {
        BusinessId = businessId;
        Status = status;
    }

    public Guid DonationId { get; } = Guid.NewGuid();

    public Guid BusinessId { get; }

    /// <summary>Estado actual de la donación en el Donations simulado.</summary>
    public string Status { get; private set; }

    /// <summary>Rol del usuario cuyo JWT reenvía Pickups en la petición actual.</summary>
    public string CallerRole { get; set; } = "Business";

    /// <summary>Cuántas veces Pickups llamó a una acción (claim, release o picked-up).</summary>
    public int CallsTo(string action) => _calls.Count(call => call == action);

    public Task<Result<DonationSummaryDTO>> GetDonationAsync(Guid donationId, CancellationToken cancellationToken = default)
    {
        if (donationId != DonationId)
        {
            return Task.FromResult(Result.Failure<DonationSummaryDTO>(
                Error.NotFound("Donations.NotFound", "La donación indicada no existe.")));
        }

        DonationSummaryDTO donation = new()
        {
            Id = DonationId,
            BusinessId = BusinessId,
            Title = "Pan del día",
            Status = Status,
            AvailableUntil = DateTime.UtcNow.AddHours(6),
            PickupAddress = "Calle 10 # 43A - 25 Sur, Medellín"
        };

        return Task.FromResult(Result.Success(donation));
    }

    public Task<Result> ClaimAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ChangeStatus(donationId, "claim", from: "Available", to: "Claimed", allowedRoles: ["Business", "Admin"]));

    public Task<Result> ReleaseAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ChangeStatus(donationId, "release", from: "Claimed", to: "Available", allowedRoles: ["Business", "Foundation", "Admin"]));

    public Task<Result> MarkPickedUpAsync(Guid donationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(ChangeStatus(donationId, "picked-up", from: "Claimed", to: "PickedUp", allowedRoles: ["Business", "Admin"]));

    private Result ChangeStatus(Guid donationId, string action, string from, string to, string[] allowedRoles)
    {
        _calls.Add(action);

        if (donationId != DonationId)
        {
            return Result.Failure(Error.NotFound("Donations.NotFound", "La donación indicada no existe."));
        }

        if (!allowedRoles.Contains(CallerRole))
        {
            return Result.Failure(Error.Forbidden("Donations.Forbidden",
                $"El rol {CallerRole} no tiene permiso para hacer {action}."));
        }

        if (Status != from)
        {
            return Result.Failure(Error.Validation("Donations.BusinessRule",
                $"No se puede hacer {action} sobre una donación en estado {Status}."));
        }

        Status = to;

        return Result.Success();
    }
}
