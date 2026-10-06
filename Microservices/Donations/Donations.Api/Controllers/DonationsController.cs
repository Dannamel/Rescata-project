using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Mvc;
using Donations.Application.UseCases.Donations.Commands.CreateDonation;
using Donations.Application.UseCases.Donations.Queries.GetDonationsList;
using Donations.Application.UseCases.Donations.Queries.GetDonationById;

namespace Donations.Api.Controllers;

[ApiController]
[Route("api/donations")]
public class DonationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDonationCommand command)
    {
        Guid donationId = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, new { id = donationId });
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
