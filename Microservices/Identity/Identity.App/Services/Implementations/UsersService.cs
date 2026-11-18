using Identity.App.DTOs.User;
using Identity.App.Entities;
using Identity.App.Responses;
using Identity.App.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.App.Services.Implementations;

public class UsersService : IUsersService
{
    private readonly UserManager<AppUser> _userManager;

    public UsersService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApiResponse<List<UserDTO>>> GetAllAsync()
    {
        List<AppUser> users = await _userManager.Users.OrderBy(user => user.Email)
                                                      .ToListAsync();

        List<UserDTO> dtos = [];

        foreach (AppUser user in users)
        {
            UserDTO dto = new()
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Roles = await _userManager.GetRolesAsync(user)
            };

            dtos.Add(dto);
        }

        return ApiResponse<List<UserDTO>>.Ok(dtos);
    }
}
