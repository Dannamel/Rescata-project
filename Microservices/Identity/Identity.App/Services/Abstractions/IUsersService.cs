using Identity.App.DTOs.User;
using Identity.App.Responses;

namespace Identity.App.Services.Abstractions;

public interface IUsersService
{
    Task<ApiResponse<List<UserDTO>>> GetAllAsync();
}
