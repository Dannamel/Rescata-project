# Rescata

Rescata conecta a negocios que publican excedentes de comida con fundaciones que programan su recogida. La app web en React + Vite envía las peticiones al API Gateway (YARP). Identity emite el JWT de sesión y cada microservicio comprueba los permisos del usuario.

| Componente | Responsabilidad | Puerto HTTP local | Base de datos |
|---|---|---|---|
| Identity (`Microservices/Identity/Identity.App`) | Usuarios y roles con ASP.NET Core Identity; login con emisión de JWT | `5223` | `RescataIdentityDb` |
| Donations (`Microservices/Donations/Donations.Api`) | Publicación, catálogo y estado de las donaciones | `5001` | `RescataDonationsDb` |
| Pickups (`Microservices/Pickups/Pickups.Api`) | Recogidas de donaciones; consulta Donations por HTTP | `5002` | `RescataPickupsDb` |
| API Gateway (`Microservices/ApiGateway/ApiGateway.App`) | Enruta `/identity`, `/donations` y `/pickups` al servicio correspondiente | `5000` | — |
| App web (`Frontend/rescata-web`) | Login, consulta de donaciones y gestión de recogidas según el rol | `5173` | — |

## Ejecución de la entrega final

### Requisitos

- .NET SDK 10
- SQL Server (los `appsettings.json` usan la instancia `.\SQLEXPRESS`)
- Herramienta de EF Core 10. Si falta, ejecutar `dotnet tool install --global dotnet-ef --version "10.*"`.
- Node.js 20.19 o posterior de la serie 20, o Node.js 22.12 en adelante, con npm.
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

Ningún servicio aplica sus migraciones al arrancar. Por eso hay que ejecutar `database update` antes de arrancar cada servicio la primera vez, y también cada vez que se actualice el código con migraciones nuevas; por ejemplo, una base de Pickups creada antes de la migración que agregó `RowVersion`. Si no hay migraciones pendientes, el comando no cambia nada, así que se puede ejecutar siempre antes de arrancar.

Con SQL Server en ejecución, abre cinco terminales de PowerShell desde la raíz del repositorio. Sigue este orden y deja cada proceso abierto: **Identity → Donations → Pickups → API Gateway → app web**. Migra Donations antes de Pickups: al actualizar una base de Pickups existente, la migración de `BusinessId` consulta `RescataDonationsDb` en la misma instancia para recuperar el negocio de cada donación.

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

**Terminal 4: API Gateway**

```powershell
dotnet run --project Microservices/ApiGateway/ApiGateway.App --launch-profile http
```

El gateway escucha en `http://localhost:5000`. Por ejemplo, al recibir `/identity/api/auth/login` elimina el prefijo `/identity` y reenvía `/api/auth/login` a Identity. Las rutas y la política CORS están en [appsettings.json del gateway](Microservices/ApiGateway/ApiGateway.App/appsettings.json).

**Terminal 5: app web**

```powershell
cd Frontend/rescata-web
npm ci
npm run dev
```

Abre [la app en localhost:5173](http://localhost:5173). Ese es el origen permitido por CORS. Si cambias la dirección del gateway, copia `.env.example` a `.env.local` en esa carpeta, ajusta `VITE_GATEWAY_URL` y reinicia Vite. Con la configuración del repositorio la app ya usa `http://localhost:5000`.

**Comprobar el flujo desde la web**

1. Inicia sesión con la cuenta de negocio de la tabla siguiente. En **Negocio**, publica una donación con una fecha límite futura.
2. En otra sesión de navegador, ingresa como fundación. En **Fundación**, busca esa donación y pulsa **Programar recogida**. Elige un horario futuro que termine antes de la fecha límite.
3. Vuelve a entrar en **Negocio** o recarga la página para ver la recogida pendiente. Al confirmarla, la donación pasa a **Reclamada** (`Claimed`). Al completarla, pasa a **Recogida** (`PickedUp`).

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

### Endpoints y roles a través del gateway

Usa `http://localhost:5000` como URL base en REST Client o Postman. Por ejemplo, `POST http://localhost:5000/identity/api/auth/login` recibe un JSON con `email` y `password` y responde con `data.accessToken`, `data.tokenType` y `data.expiresAtUtc`. Ese token se envía a los demás endpoints en el encabezado `Authorization: Bearer <token>`.

| Servicio | Endpoint | Acceso |
|---|---|---|
| Identity | `POST /identity/api/auth/login` | Público |
| Identity | `GET /identity/api/users` | `Admin` |
| Donations | `GET /donations/api/food-categories`, `GET /donations/api/donations`, `GET /donations/api/donations/{id}` | Cualquier usuario autenticado |
| Donations | `POST /donations/api/donations`, `PUT /donations/api/donations/{id}`, `PATCH /donations/api/donations/{id}/cancel` | `Business` para sus donaciones, o `Admin` |
| Donations | `PATCH /donations/api/donations/{id}/claim`, `PATCH /donations/api/donations/{id}/picked-up` | `Business` dueño o `Admin` |
| Donations | `PATCH /donations/api/donations/{id}/release` | `Business` dueño, `Foundation` o `Admin` |
| Pickups | `GET /pickups/api/pickups`, `GET /pickups/api/pickups/{id}` | La fundación ve las que programó, el negocio las de sus donaciones y el administrador todas |
| Pickups | `POST /pickups/api/pickups`, `PUT /pickups/api/pickups/{id}`, `PATCH /pickups/api/pickups/{id}/cancel` | `Foundation` para sus recogidas, o `Admin` |
| Pickups | `PATCH /pickups/api/pickups/{id}/confirm`, `PATCH /pickups/api/pickups/{id}/complete` | `Business` dueño de la donación o `Admin` |

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

- **Id de una categoría**: `GET /donations/api/food-categories` con cualquier token válido.
- **Id del usuario Business**: para los ejemplos que lo piden, se obtiene con `GET /identity/api/users` como Admin o con la consulta SQL de arriba. Al publicar como Business, Donations toma el dueño del JWT.

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
  "availableUntil": "<fecha y hora futura en UTC, por ejemplo 2 horas después de la actual>"
}
```

`availableUntil` debe ser una fecha futura; con una fecha pasada la respuesta es `400`. El caso 1 de `Donations.Api/Donations.Api.http` genera esa fecha con `{{$datetime iso8601 2 h}}`, así que no hay que escribirla a mano.

Los enums se pueden enviar como texto (`"Kilogramos"`) o como número (`1`). Obtén un ID válido de categoría con `GET /donations/api/food-categories` antes de probar el ejemplo.

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
| `ForbiddenException` | El negocio intenta gestionar una donación ajena | `403`, `error: Forbidden` |
| Cualquier otra | Error inesperado | `500`, mensaje genérico; el detalle queda en el log y no se expone al cliente |

Para que un nuevo caso de uso tenga validación basta con crear su `AbstractValidator<TCommand>`: se registra solo con `AddValidatorsFromAssemblyContaining` y el mediador lo ejecuta antes del handler.
