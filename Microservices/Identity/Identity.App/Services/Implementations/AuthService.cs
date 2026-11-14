using Identity.App.DTOs.Auth;
using Identity.App.Entities;
using Identity.App.Responses;
using Identity.App.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.App.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<AppUser> userManager,
                       SignInManager<AppUser> signInManager,
                       IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponseDTO>> LoginAsync(LoginDTO dto)
    {
        AppUser? user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null || !user.IsActive)
        {
            return InvalidCredentials();
        }

        SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);

        IConfigurationSection jwt = _configuration.GetSection("Jwt");
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(jwt.GetValue<int>("ExpirationMinutes", 60));

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email!),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        ];

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        JwtSecurityToken token = new(issuer: jwt["Issuer"],
                                     audience: jwt["Audience"],
                                     claims: claims,
                                     expires: expiresAt,
                                     signingCredentials: credentials);

        AuthResponseDTO response = new()
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAt
        };

        return ApiResponse<AuthResponseDTO>.Ok(response, "Inicio de sesión exitoso.");
    }

    private static ApiResponse<AuthResponseDTO> InvalidCredentials()
    {
        ApiError error = new("Auth.InvalidCredentials", "El correo o la contraseña son incorrectos.", "Unauthorized");

        return ApiResponse<AuthResponseDTO>.Fail([error], "No fue posible iniciar sesión.");
    }
}
