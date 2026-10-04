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
