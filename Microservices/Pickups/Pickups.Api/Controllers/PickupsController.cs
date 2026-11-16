using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pickups.Api.Core;
using Pickups.Api.Core.Pagination;
using Pickups.Api.DTOs;
using Pickups.Api.Extensions;
using Pickups.Api.Services.Pickups;

namespace Pickups.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pickups")]
public sealed class PickupsController(IPickupService pickupService) : ControllerBase
{
    private const string FoundationOrAdmin = "Foundation,Admin";
    private const string BusinessOrAdmin = "Business,Admin";

    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    [Authorize(Roles = FoundationOrAdmin)]
    public Task<IActionResult> Create([FromBody] CreatePickupDTO dto) =>
        ForCurrentUserAsync(
            userId => pickupService.CreateAsync(userId, dto),
            pickup => CreatedAtAction(nameof(GetById), new { id = pickup.Id }, pickup));

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize)
    {
        PaginationRequest request = new(pageNumber, pageSize);

        if ((long)(request.PageNumber - 1) * request.PageSize > int.MaxValue)
        {
            return Error.Validation("Pickups.InvalidPagination", "El número de página es demasiado grande.")
                .ToErrorResult();
        }

        return await ForCurrentUserAsync(userId => pickupService.GetPaginationAsync(request, userId, IsAdmin));
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id) =>
        ForCurrentUserAsync(userId => pickupService.GetOneAsync(id, userId, IsAdmin));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = FoundationOrAdmin)]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdatePickupDTO dto) =>
        ForCurrentUserAsync(userId => pickupService.UpdateAsync(id, userId, dto, IsAdmin));

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = FoundationOrAdmin)]
    public Task<IActionResult> Cancel(Guid id, [FromBody] CancelPickupDTO dto) =>
        ForCurrentUserAsync(userId => pickupService.CancelAsync(id, userId, dto, IsAdmin));

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = BusinessOrAdmin)]
    public Task<IActionResult> Confirm(Guid id) =>
        ForCurrentUserAsync(userId => pickupService.ConfirmAsync(id, userId, IsAdmin));

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = BusinessOrAdmin)]
    public Task<IActionResult> Complete(Guid id) =>
        ForCurrentUserAsync(userId => pickupService.CompleteAsync(id, userId, IsAdmin));

    private async Task<IActionResult> ForCurrentUserAsync<T>(
        Func<Guid, Task<Result<T>>> action,
        Func<T, IActionResult>? onSuccess = null)
    {
        Guid userId = User.GetUserId();

        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        Result<T> result = await action(userId);

        return result.IsSuccess && onSuccess is not null
            ? onSuccess(result.Value)
            : result.ToActionResult();
    }
}
