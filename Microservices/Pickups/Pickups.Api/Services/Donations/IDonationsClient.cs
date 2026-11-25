using Pickups.Api.Core;

namespace Pickups.Api.Services.Donations;

public interface IDonationsClient
{
    Task<Result<DonationSummaryDTO>> GetDonationAsync(Guid donationId, CancellationToken cancellationToken = default);

    /// <summary>Reserva la donación (Available → Claimed) cuando el negocio confirma la recogida.</summary>
    Task<Result> ClaimAsync(Guid donationId, CancellationToken cancellationToken = default);

    /// <summary>Devuelve la donación a disponible (Claimed → Available) cuando se cancela una recogida confirmada.</summary>
    Task<Result> ReleaseAsync(Guid donationId, CancellationToken cancellationToken = default);

    /// <summary>Marca la donación como recogida (Claimed → PickedUp) cuando se completa la recogida.</summary>
    Task<Result> MarkPickedUpAsync(Guid donationId, CancellationToken cancellationToken = default);
}
