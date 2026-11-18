using Identity.App.DTOs.Auth;
using Identity.App.Responses;

namespace Identity.App.Services.Abstractions;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto);
}
