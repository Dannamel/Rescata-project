using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Mvc;
using Donations.Application.UseCases.Donations.Commands.CancelDonation;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;
using Donations.Application.UseCases.Donations.Commands.UpdateDonation;

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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDonationCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);

        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _mediator.Send(new CancelDonationCommand(id));

        return NoContent();
    }
}
