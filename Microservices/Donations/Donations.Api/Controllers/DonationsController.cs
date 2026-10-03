using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Donations.Api.Controllers;

[ApiController]
[Route("api/donations")]
public class DonationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // Las acciones (POST, GET, PUT, PATCH) las agrega cada integrante al final de esta clase.
}
