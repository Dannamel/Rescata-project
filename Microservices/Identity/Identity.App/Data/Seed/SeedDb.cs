
using Identity.App.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.App.Data.Seed;

public static class SeedDb
{
    public static async Task InitializeAsync(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IConfiguration configuration)
    {
        var users = new[]
        {
            new
            {
                FirstName = "Admin",
                LastName = "Test",
                UserName = "admin.test",
                Email = "admin.test@rescata.local",
                Role = "Admin",
                PasswordKey = "SeedUsers:AdminPassword"
            },
            new
            {
                FirstName = "Business",
                LastName = "Test",
                UserName = "business.test",
                Email = "business.test@rescata.local",
                Role = "Business",
                PasswordKey = "SeedUsers:BusinessPassword"
            },
            new
            {
                FirstName = "Foundation",
                LastName = "Test",
                UserName = "foundation.test",
                Email = "foundation.test@rescata.local",
                Role = "Foundation",
                PasswordKey = "SeedUsers:FoundationPassword"
            }
        };

        foreach (var userData in users)
        {
            if (!await roleManager.RoleExistsAsync(userData.Role))
            {
                var roleResult = await roleManager.CreateAsync(
                    new AppRole { Name = userData.Role });

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Error creando el rol {userData.Role}: " +
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            var user = await userManager.FindByEmailAsync(userData.Email);

            if (user is null)
            {
                var password = configuration[userData.PasswordKey];

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        $"Falta configurar la contraseña para {userData.UserName}.");
                }

                user = new AppUser
                {
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    UserName = userData.UserName,
                    Email = userData.Email,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Error creando {userData.UserName}: " +
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, userData.Role))
            {
                var assignResult = await userManager.AddToRoleAsync(
                    user, userData.Role);

                if (!assignResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Error asignando {userData.Role} a {userData.UserName}: " +
                        string.Join(", ", assignResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
