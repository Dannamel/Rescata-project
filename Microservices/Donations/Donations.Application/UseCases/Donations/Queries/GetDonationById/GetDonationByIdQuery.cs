using Donations.Application.Utilities.Mediator;

namespace Donations.Application.UseCases.Donations.Queries.GetDonationById;

public sealed class GetDonationByIdQuery : IRequest<DonationDetailDTO>
{
    public Guid Id { get; set; }

    public GetDonationByIdQuery(Guid id)
    {
        Id = id;
    }
}
