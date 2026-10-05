# Rescata

Plataforma de rescate de alimentos — Entrega 1: microservicio **Donations**.

## Base de datos y migraciones

El microservicio Donations usa **SQL Server** con **Entity Framework Core 10** (enfoque Code First). La capa `Donations.Persistence` contiene el `DataContext`, las configuraciones Fluent API, los repositorios, el Unit of Work y los seeders.

### Modelo físico

| Tabla | Origen | Notas |
|---|---|---|
| `Donations` | Entidad `Donation` | `Status` y `Quantity_Unit` se guardan como texto. `Quantity` y `PickupAddress` son value objects mapeados con `OwnsOne`, por eso sus columnas quedan dentro de la misma tabla (`Quantity_Amount`, `PickupAddress_RoadType`, `PickupAddress_City`, ...). |
| `FoodCategories` | Entidad `FoodCategory` | Catálogo sembrado al arrancar. Relación 1:N con `Donations` (`DeleteBehavior.Restrict`). |

### Requisitos

- .NET SDK 10
- SQL Server
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`

### Cadena de conexión

En `Donations.Api/appsettings.json`, cada integrante ajusta `ConnectionStrings:MyConnection` a su servidor local:

```json
"ConnectionStrings": {
  "MyConnection": "Server=.;Database=RescataDonationsDb;Trusted_Connection=True;encrypt=false"
}
```

Con usuario y contraseña de SQL Server: `Server=.\\INSTANCIA;Database=RescataDonationsDb;User Id=usuario;Password=clave;encrypt=false`.

### Migraciones

Desde la carpeta `Microservices/Donations`:

```bash
ef.cmd add NombreMigracion   # crea una migración en Donations.Persistence/Migrations
ef.cmd update                # aplica las migraciones y crea RescataDonationsDb
ef.cmd remove                # elimina la última migración (si no se ha aplicado)
```

### Datos iniciales (seeders)

Al arrancar la Api, `DataBaseSeeder.SeedAsync` ejecuta en orden todos los `IDataSeeder` registrados. `FoodCategorySeeder` carga las categorías Frutas y verduras, Panadería, Lácteos, Comidas preparadas, Enlatados y secos, y Carnes y proteínas. Si la tabla ya tiene datos, no vuelve a insertarlos.

## Crear donación

`POST /api/donations` publica el excedente de comida de un negocio. Responde `201 Created` con el Id de la donación.

Flujo: `DonationsController` → `IMediator` → `CreateDonationCommandValidator` → `CreateDonationUseCase` → `Donation` (dominio) → `IDonationsRepository.CreateAsync` → `IUnitOfWork.CommitAsync`.

- El mediador valida el command con FluentValidation antes de ejecutar el caso de uso.
- El caso de uso verifica que la categoría exista; el dominio da por hecho que los Id que recibe existen.
- El repositorio solo prepara la inserción; el Unit of Work la ejecuta con `SaveChangesAsync`.

Ejemplo de petición:

```json
{
  "businessId": "2c1f7c1e-5a7b-4e8a-9a3b-1d2e3f4a5b6c",
  "title": "Pan del día",
  "description": "20 kg de pan francés horneado hoy, en buen estado.",
  "quantityAmount": 20,
  "quantityUnit": "Kilogramos",
  "foodCategoryId": "<Id de una categoría de FoodCategories>",
  "pickupAddress": {
    "roadType": "Calle",
    "roadNumber": "10",
    "crossRoadNumber": "43A",
    "plateNumber": "25",
    "roadSuffix": "Sur",
    "city": "Medellín"
  },
  "availableUntil": "2026-10-07T18:00:00Z"
}
```

Los enums se pueden enviar como texto (`"Kilogramos"`) o como número (`1`). Los Id de las categorías se generan al sembrar la base, así que hay que consultarlos en `FoodCategories` antes de probar.

## Manejo de errores

`ExceptionMiddleware` captura las excepciones de todos los endpoints y responde siempre con el mismo formato:

```json
{
  "status": 400,
  "error": "Validation",
  "message": "Uno o más datos no son válidos.",
  "errors": [ "La cantidad debe ser mayor a cero." ]
}
```

| Excepción | Origen | Respuesta |
|---|---|---|
| `CustomValidationException` | Validadores de FluentValidation (en el mediador) | `400`, `error: Validation`, lista en `errors` |
| `BussinesRuleException` | Reglas del dominio | `400`, `error: BusinessRule` |
| `NotFoundException` | Casos de uso, cuando el recurso no existe | `404`, `error: NotFound` |
| Cualquier otra | Error inesperado | `500`, mensaje genérico; el detalle queda en el log y no se expone al cliente |

Para que un nuevo caso de uso tenga validación basta con crear su `AbstractValidator<TCommand>`: se registra solo con `AddValidatorsFromAssemblyContaining` y el mediador lo ejecuta antes del handler.