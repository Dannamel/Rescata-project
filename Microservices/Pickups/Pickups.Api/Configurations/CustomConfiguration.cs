using Microsoft.EntityFrameworkCore;
using Pickups.Api.Data;
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

        // DataContext configuration
        builder.Services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("PickupsConnection"));
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


        app.MapControllers();

        return app;
    }
}
