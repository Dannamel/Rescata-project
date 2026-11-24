using Donations.Application.UseCases.FoodCategories.Queries.GetFoodCategoriesList;
using Donations.Application.Utilities.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Donations.Api.Controllers;

[ApiController]
[Route("api/food-categories")]
[Authorize]
public class FoodCategoriesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var categories = await _mediator.Send(new GetFoodCategoriesListQuery());

        return Ok(categories);
    }
}
