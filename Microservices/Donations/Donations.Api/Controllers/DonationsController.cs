using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Mvc;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;

namespace Donations.Api.Controllers;

[ApiController]
[Route("api/donations")]
public class DonationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // Las acciones (POST, GET, PUT, PATCH) las agrega cada integrante al final de esta clase.

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDonationCommand command)
    {
        Guid donationId = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, new { id = donationId });
    }
}
