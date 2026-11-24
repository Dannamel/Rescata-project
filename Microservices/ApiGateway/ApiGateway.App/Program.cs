using ApiGateway.App.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomConfiguration();

var app = builder.Build();

app.UseCustomConfiguration();

app.Run();
