# Rescata

Plataforma de rescate de alimentos. Los negocios publican sus excedentes de comida (Donations) y las fundaciones programan su recogida (Pickups). Identity autentica a los usuarios y emite el JWT que validan los otros dos servicios.

| Microservicio | Responsabilidad | Puerto (perfil `http`) | Base de datos |
|---|---|---|---|
| Identity (`Microservices/Identity/Identity.App`) | Usuarios y roles con ASP.NET Core Identity; login con emisión de JWT | `5223` | `RescataIdentityDb` |
| Donations (`Microservices/Donations/Donations.Api`) | Donaciones de alimentos (Entrega 1) | `5001` | `RescataDonationsDb` |
| Pickups (`Microservices/Pickups/Pickups.Api`) | Recogidas de donaciones; consulta Donations por HTTP | `5002` | `RescataPickupsDb` |

## Ejecución (Entrega 2)

### Requisitos

- .NET SDK 10
- SQL Server (los `appsettings.json` usan la instancia `.\SQLEXPRESS`)
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`
- Para las pruebas: extensión REST Client de VS Code (archivos `.http`) o Postman

### Configuración

**JWT.** Los tres servicios comparten `Jwt:Issuer` (`Rescata.Identity`), `Jwt:Audience` (`Rescata.Services`) y `Jwt:SecretKey`. Identity firma el token con esa clave y Donations y Pickups validan firma, emisor, audiencia y vigencia. Si se cambia la clave, hay que cambiarla en los tres `appsettings.json`. La vigencia del token es `Jwt:ExpirationMinutes` en Identity (60 minutos).

**Contraseñas de los usuarios sembrados.** No están en el repositorio. `SeedDb` las lee de `SeedUsers:AdminPassword`, `SeedUsers:BusinessPassword` y `SeedUsers:FoundationPassword`, y si falta alguna, Identity no arranca. Se definen como variables de entorno antes de arrancar Identity por primera vez (ver abajo). Deben tener mínimo 8 caracteres, un número, una mayúscula y una minúscula. Solo se usan cuando el usuario todavía no existe.

**Cadenas de conexión.** Si SQL Server no está en `.\SQLEXPRESS`, no hace falta editar los `appsettings.json`: se sobrescribe la cadena con una variable de entorno en la misma terminal donde se aplica la migración y se arranca el servicio. Las variables de entorno solo duran mientras la terminal esté abierta.

| Servicio | Variable de entorno |
|---|---|
| Identity | `ConnectionStrings__IdentityConnection` |
| Donations | `ConnectionStrings__MyConnection` |
| Pickups | `ConnectionStrings__PickupsConnection` |

### Crear las bases y arrancar los servicios

Ningún servicio aplica sus migraciones al arrancar, así que cada uno necesita su `database update` la primera vez. Se usa una terminal de PowerShell por servicio, desde la raíz del repositorio, y se arrancan en este orden.

**Terminal 1: Identity**

```powershell
$env:SeedUsers__AdminPassword = "<contraseña>"
$env:SeedUsers__BusinessPassword = "<contraseña>"
$env:SeedUsers__FoundationPassword = "<contraseña>"
# Solo si SQL Server no está en .\SQLEXPRESS:
# $env:ConnectionStrings__IdentityConnection = "Server=.;Database=RescataIdentityDb;Trusted_Connection=True;TrustServerCertificate=True;"

dotnet ef database update --project Microservices/Identity/Identity.App
dotnet run --project Microservices/Identity/Identity.App --launch-profile http
```

Al arrancar, `SeedDb` crea los roles `Admin`, `Business` y `Foundation` y un usuario por rol.

**Terminal 2: Donations**

```powershell
# Solo si SQL Server no está en .\SQLEXPRESS:
# $env:ConnectionStrings__MyConnection = "Server=.;Database=RescataDonationsDb;Trusted_Connection=True;TrustServerCertificate=True;"

dotnet ef database update --project Microservices/Donations/Donations.Persistence --startup-project Microservices/Donations/Donations.Api
dotnet run --project Microservices/Donations/Donations.Api --launch-profile http
```

Al arrancar, `FoodCategorySeeder` carga las categorías de alimentos.

**Terminal 3: Pickups**

```powershell
# Solo si SQL Server no está en .\SQLEXPRESS:
# $env:ConnectionStrings__PickupsConnection = "Server=.;Database=RescataPickupsDb;Trusted_Connection=True;TrustServerCertificate=True;"

dotnet ef database update --project Microservices/Pickups/Pickups.Api
dotnet run --project Microservices/Pickups/Pickups.Api --launch-profile http
```

Pickups llama a Donations en `Services:DonationsBaseUrl` (`http://localhost:5001`) y le reenvía el JWT de la petición.

### Usuarios sembrados

| Correo | Rol | Contraseña |
|---|---|---|
| `admin.test@rescata.local` | `Admin` | `SeedUsers__AdminPassword` |
| `business.test@rescata.local` | `Business` | `SeedUsers__BusinessPassword` |
| `foundation.test@rescata.local` | `Foundation` | `SeedUsers__FoundationPassword` |

Para comprobar los usuarios y sus roles en `RescataIdentityDb`:

```sql
SELECT u.Id, u.Email, r.Name AS RoleName
FROM AspNetUsers AS u
JOIN AspNetUserRoles AS ur ON ur.UserId = u.Id
JOIN AspNetRoles AS r ON r.Id = ur.RoleId;
```

Las contraseñas quedan guardadas como hash en `AspNetUsers.PasswordHash`, no en texto plano.

### Endpoints y roles

`POST /api/auth/login` (Identity) recibe `email` y `password` y responde con `data.accessToken`, `data.tokenType` y `data.expiresAtUtc`. Ese token se envía a los demás endpoints en el encabezado `Authorization: Bearer <token>`.

| Servicio | Endpoint | Acceso |
|---|---|---|
| Identity | `POST /api/auth/login` | Público |
| Identity | `GET /api/users` | `Admin` |
| Donations | `GET /api/donations`, `GET /api/donations/{id}` | Cualquier usuario autenticado |
| Donations | `POST /api/donations`, `PUT /api/donations/{id}`, `PATCH /api/donations/{id}/cancel` | `Business`, `Admin` |
| Pickups | `GET /api/pickups`, `GET /api/pickups/{id}` | Cualquier usuario autenticado; quien no es Admin solo ve sus recogidas |
| Pickups | `POST /api/pickups`, `PUT /api/pickups/{id}`, `PATCH /api/pickups/{id}/cancel` | `Foundation`, `Admin` |
| Pickups | `PATCH /api/pickups/{id}/confirm`, `PATCH /api/pickups/{id}/complete` | `Business` dueño de la donación, `Admin` |

- **401**: la petición no trae un JWT válido (falta, firma alterada o vencido).
- **403**: el JWT es válido, pero el usuario no tiene el rol requerido, o en Pickups no es el dueño del recurso.

Reglas de Pickups: la donación debe existir en Donations, estar `Available` y no estar vencida; la recogida debe ser futura y terminar antes de `AvailableUntil`; una donación solo puede tener una recogida activa (`Pending` o `Confirmed`). Los estados avanzan `Pending → Confirmed → Completed`, y se puede cancelar mientras esté `Pending` o `Confirmed`. Si dos operaciones modifican la misma recogida a la vez, la segunda recibe `409`.

### Pruebas

| Archivo | Contenido |
|---|---|
| `Microservices/Identity/Identity.App/Identity.App.http` | Login válido e inválido; `/api/users` con token de Admin (200), sin token (401) y con token de Business (403) |
| `Microservices/Donations/Donations.Api/Donations.Security.http` | Los ocho escenarios de seguridad del video sobre Donations |
| `Microservices/Donations/Donations.Api/Donations.Api.http` y `Rescata-Donations.postman_collection.json` | Pruebas funcionales de Donations con token de Business |
| `Microservices/Pickups/Pickups.Api/Pickups.Api.http` | Flujo completo: crear donación, programar, reprogramar, cancelar, confirmar y completar la recogida |

Los `.http` piden las contraseñas al ejecutarse (`@prompt`). Dos datos se generan al sembrar las bases y hay que consultarlos antes de probar:

- **Id de una categoría**: `SELECT Id, Name FROM FoodCategories;` en `RescataDonationsDb`.
- **Id del usuario Business**: `GET /api/users` con token de Admin, o la consulta SQL de arriba. Para que el negocio pueda confirmar y completar una recogida, la donación debe crearse con ese Id en `businessId`.

### Escenarios del video

| N.º | Escenario | Dónde |
|---|---|---|
| 1 | Ejecución de los microservicios | Las tres terminales de arriba, cada una escuchando en su puerto |
| 2 | Inicialización de Identity con seeders | Consulta SQL de usuarios y roles |
| 3 | Autenticación y emisión del JWT | Login en `Identity.App.http` o paso 3 de `Donations.Security.http` |
| 4 | Acceso sin token (401) | Paso 4 de `Donations.Security.http`; paso 1 de `Pickups.Api.http` |
| 5 | Acceso con token válido | Paso 5 de `Donations.Security.http` |
| 6 | Autorización por roles | Paso 6 de `Donations.Security.http`; pasos 5 y 12 de `Pickups.Api.http` |
| 7 | Acceso sin el rol requerido (403) | Paso 7 de `Donations.Security.http`; paso 11 de `Pickups.Api.http`; `/api/users` con token de Business |
| 8 | Token inválido o expirado (401) | Pasos 8a y 8b de `Donations.Security.http` |

Para el token expirado (8b), Identity se reinicia con `$env:Jwt__ExpirationMinutes = "1"`, se hace login y se espera a que pase `expiresAtUtc` más los 30 segundos de tolerancia (`ClockSkew`). Después se vuelve a arrancar Identity sin esa variable.

## Donations: base de datos y migraciones

El microservicio Donations usa **SQL Server** con **Entity Framework Core 10** (enfoque Code First). La capa `Donations.Persistence` contiene el `DataContext`, las configuraciones Fluent API, los repositorios, el Unit of Work y los seeders.

### Modelo físico

| Tabla | Origen | Notas |
|---|---|---|
| `Donations` | Entidad `Donation` | `Status` y `Quantity_Unit` se guardan como texto. `Quantity` y `PickupAddress` son value objects mapeados con `OwnsOne`, por eso sus columnas quedan dentro de la misma tabla (`Quantity_Amount`, `PickupAddress_RoadType`, `PickupAddress_City`, ...). |
| `FoodCategories` | Entidad `FoodCategory` | Catálogo sembrado al arrancar. Relación 1:N con `Donations` (`DeleteBehavior.Restrict`). |

### Cadena de conexión

En `Donations.Api/appsettings.json`, la cadena `ConnectionStrings:MyConnection` apunta a la instancia `.\SQLEXPRESS`:

```json
"ConnectionStrings": {
  "MyConnection": "Server=.\\SQLEXPRESS;Database=RescataDonationsDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Con otra instancia se usa la variable de entorno `ConnectionStrings__MyConnection` (ver [Configuración](#configuración)). Con usuario y contraseña de SQL Server: `Server=.\\INSTANCIA;Database=RescataDonationsDb;User Id=usuario;Password=clave;TrustServerCertificate=True`.

### Migraciones

Desde la carpeta `Microservices/Donations`:

```bash
ef.cmd add NombreMigracion   # crea una migración en Donations.Persistence/Migrations
ef.cmd update                # aplica las migraciones y crea RescataDonationsDb
ef.cmd remove                # elimina la última migración (si no se ha aplicado)
```

### Datos iniciales (seeders)

Al arrancar la Api, `DataBaseSeeder.SeedAsync` ejecuta en orden todos los `IDataSeeder` registrados. `FoodCategorySeeder` carga las categorías Frutas y verduras, Panadería, Lácteos, Comidas preparadas, Enlatados y secos, y Carnes y proteínas. Si la tabla ya tiene datos, no vuelve a insertarlos.

## Donations: crear donación

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

## Donations: manejo de errores

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
