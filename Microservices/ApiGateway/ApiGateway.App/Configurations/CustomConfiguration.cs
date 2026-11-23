namespace ApiGateway.App.Configurations;

public static class CustomConfiguration
{
    public const string FrontendCorsPolicy = "Frontend";

    public static WebApplicationBuilder AddCustomConfiguration(this WebApplicationBuilder builder)
    {
        builder.AddFrontendCors();

        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        return builder;
    }

    public static WebApplicationBuilder AddFrontendCors(this WebApplicationBuilder builder)
    {
        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Falta configurar Cors:AllowedOrigins en appsettings.json.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return builder;
    }

    public static WebApplication UseCustomConfiguration(this WebApplication app)
    {
        app.UseCors();

        app.MapReverseProxy();

        return app;
    }
}
