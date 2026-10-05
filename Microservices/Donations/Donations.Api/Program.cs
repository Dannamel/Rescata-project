using System.Text.Json.Serialization;
using Donations.Api.Middlewares;
using Donations.Application;
using Donations.Persistence;
using Donations.Persistence.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Las validaciones de entrada las hace FluentValidation en la capa de aplicación,
    // así todas las respuestas de error tienen el mismo formato.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    // Permite enviar y recibir los enums como texto (ej. "Kilogramos") además de números.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);

var app = builder.Build();

// Debe ir primero en el pipeline para capturar las excepciones de todo lo que sigue.
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await DataBaseSeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();