using Identity.App.DTOs.Auth;
using Identity.App.Responses;
using Identity.App.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Identity.App.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        ApiResponse<AuthResponseDTO> response = await _authService.LoginAsync(dto);

        if (!response.IsSuccess)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }
}
