using Identity.App.DTOs.User;
using Identity.App.Responses;
using Identity.App.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.App.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        ApiResponse<List<UserDTO>> response = await _usersService.GetAllAsync();

        return Ok(response);
    }
}
