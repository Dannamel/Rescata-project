using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pickups.Api.Data;
using Pickups.Api.Services.Donations;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace Pickups.Api.Configurations;

public static class CustomConfiguration
{

    public static WebApplicationBuilder AddCustomConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("PickupsConnection"));
        });

        builder.AddIAM();

        builder.AddDonationsClient();

        return builder;
    }

    public static WebApplicationBuilder AddIAM(this WebApplicationBuilder builder)
    {
        IConfigurationSection jwt = builder.Configuration.GetSection("Jwt");

        string secretKey = jwt["SecretKey"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:SecretKey en appsettings.json.");

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(secretKey));

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],

                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    public static WebApplicationBuilder AddDonationsClient(this WebApplicationBuilder builder)
    {
        string donationsBaseUrl = builder.Configuration["Services:DonationsBaseUrl"]
            ?? throw new InvalidOperationException("Falta configurar Services:DonationsBaseUrl en appsettings.json.");

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHttpClient<IDonationsClient, DonationsClient>(client =>
        {
            client.BaseAddress = new Uri(donationsBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return builder;
    }

    public static WebApplication UseCustomConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
