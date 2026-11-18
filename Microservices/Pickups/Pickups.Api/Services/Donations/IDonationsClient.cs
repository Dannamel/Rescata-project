using Pickups.Api.Core;

namespace Pickups.Api.Services.Donations;

public interface IDonationsClient
{
    Task<Result<DonationSummaryDTO>> GetDonationAsync(Guid donationId, CancellationToken cancellationToken = default);
}
