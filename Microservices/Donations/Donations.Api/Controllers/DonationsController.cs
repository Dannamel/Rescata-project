using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Donations.Application.UseCases.Donations.Commands.CancelDonation;
using Donations.Application.UseCases.Donations.Commands.ClaimDonation;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;
using Donations.Application.UseCases.Donations.Commands.MarkDonationPickedUp;
using Donations.Application.UseCases.Donations.Commands.ReleaseDonation;
using Donations.Application.UseCases.Donations.Commands.UpdateDonation;
using Donations.Application.UseCases.Donations.Queries.GetDonationsList;
using Donations.Application.UseCases.Donations.Queries.GetDonationById;

namespace Donations.Api.Controllers;

[ApiController]
[Route("api/donations")]
[Authorize]
public class DonationsController(IMediator mediator) : ControllerBase
{
    private const string BusinessOrAdmin = "Business,Admin";

    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateDonationCommand command)
    {
        Guid donationId = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, new { id = donationId });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDonationCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);

        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        await _mediator.Send(new CancelDonationCommand(id));

        return NoContent();
    }

    [HttpPatch("{id:guid}/claim")]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> Claim([FromRoute] Guid id)
    {
        await _mediator.Send(new ClaimDonationCommand(id));

        return NoContent();
    }

    [HttpPatch("{id:guid}/release")]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> Release([FromRoute] Guid id)
    {
        await _mediator.Send(new ReleaseDonationCommand(id));

        return NoContent();
    }

    [HttpPatch("{id:guid}/picked-up")]
    [Authorize(Roles = BusinessOrAdmin)]
    public async Task<IActionResult> MarkPickedUp([FromRoute] Guid id)
    {
        await _mediator.Send(new MarkDonationPickedUpCommand(id));

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetDonationsListQuery query)
    {
        var donations = await _mediator.Send(query);

        return Ok(donations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var donation = await _mediator.Send(new GetDonationByIdQuery(id));

        return Ok(donation);
    }
}
