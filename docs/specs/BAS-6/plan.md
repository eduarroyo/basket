---
codigo: BAS-6
estado: Planificado
tags:
  - plan
---

# BAS-6: Plan técnico

## Entidades del modelo de datos afectadas

`Temporada`, `Categoria`, `Club`, `Sede` (BAS-5) — sin cambios de esquema, solo pantallas sobre las entidades ya existentes.

## Pantallas afectadas

De `screens.md`, sección "Área admin" → "Catálogo": `Temporadas`, `Categorías`, `Clubes`, `Sedes`. Más una página de inicio del área Admin, no listada explícitamente en `screens.md` (necesaria como punto de entrada, ver `spec.md`).

## Decisiones técnicas específicas de este incremento

### Scaffolding como punto de partida, no como resultado final

Cada entidad se genera con:

```bash
dotnet aspnet-codegenerator razorpage -m <Entidad> -dc ApplicationDbContext -udl -outDir Areas/Admin/Pages/<Entidad> --referenceScriptLibraries
```

El scaffolding genera por defecto `Index`, `Create`, `Edit`, `Details` y `Delete`. Se borran `Details.cshtml(.cs)` y `Delete.cshtml(.cs)` de cada carpeta tras generar (fuera de alcance, ver `spec.md`) en vez de intentar que la herramienta no los genere — no expone esa granularidad. El resto (`Index`/`Create`/`Edit`) se revisa a mano: nombres de campos, `[Display(Name = "...")]` en español donde el nombre de la propiedad no sea ya autoexplicativo, y que la validación generada coincida con las restricciones de `Data/Configurations/*Configuration.cs` (BAS-5) — el scaffolding lee el modelo de EF Core, así que en general ya coincide, pero se revisa igual.

**Validación**: el scaffolding no genera `[Required]`/`[StringLength]` en las entidades (esas restricciones solo existían como configuración Fluent API de EF Core, que no participa en `ModelState.IsValid` de Razor Pages) ni resuelve bien el desplegable de `Temporada.Estado` (genera un `<select>` vacío porque la propiedad está mapeada como `string` vía `HasConversion<string>()`, no reconocible como enum por la herramienta). Se añaden a mano las anotaciones de `System.ComponentModel.DataAnnotations` en `Data/Entities/*.cs` (duplican el límite ya declarado en la configuración de EF Core, aceptado — es la misma razón por la que se descartó una capa de DTO) y `asp-items="Html.GetEnumSelectList<TemporadaEstado>()"` en el `<select>` de `Create`/`Edit`.

**Colisión de namespace nombre-de-carpeta vs. nombre-de-tipo**: cada carpeta de entidad (`Areas/Admin/Pages/Temporada/`, etc.) coincide con el nombre de la clase de la entidad (`Temporada`). Razor deriva el namespace de las vistas de la ruta de carpetas (`_ViewImports.cshtml` + convención de subcarpeta), así que sin intervención generaría `BasketBaseTracker.Web.Areas.Admin.Pages.Temporada` — un namespace anidado literalmente llamado `Temporada`, que oscurece el tipo `Temporada` importado vía `using BasketBaseTracker.Web.Data.Entities;` en cualquier fichero bajo `...Pages` (error `CS0118`, "'Temporada' es espacio de nombres pero se usa como tipo"). Se resuelve poniendo `@namespace BasketBaseTracker.Web.Areas.Admin.Pages.Temporadas` (plural) explícito en cada `.cshtml`, a juego con el namespace ya plural de su `PageModel` (`.cshtml.cs`) — nombre de carpeta física en singular (coincide con la entidad, más natural), namespace de código en plural (evita la colisión), sin relación entre ambos más que la convención de esta carpeta.

### Sin capa de ViewModel/DTO

Las páginas enlazan directamente contra las clases de `Data/Entities/*.cs` (`[BindProperty] public Categoria Categoria { get; set; }`, patrón por defecto del scaffolding) — no se introduce una capa de DTOs para estas cuatro pantallas simples de catálogo, coherente con la filosofía de simplicidad del proyecto (`functional.md#recursos-de-desarrollo`) y con que estas entidades no tienen ningún campo sensible que deba ocultarse del formulario.

### Autorización: sin cambios, ya cubierta por convención existente

`Program.cs` ya aplica `AuthorizeAreaFolder("Admin", "/", "Administrador")` a toda el área Admin (BAS-3), así que las páginas nuevas quedan protegidas automáticamente en cuanto se colocan bajo `Areas/Admin/Pages/`. No hace falta ningún atributo `[Authorize]` adicional por página.

### Layout propio del área Admin

`Areas/Admin/Pages/Shared/_Layout.cshtml`: copia mínima del layout público (mismo Bootstrap, mismo `site.css`) con una barra de navegación propia — enlaces a las cuatro pantallas de catálogo y "Cerrar sesión" (`POST` a `/Admin/Logout`, como ya hace `Logout.cshtml` existente). Se actualiza `Areas/Admin/Pages/_ViewStart.cshtml` para apuntar a este layout nuevo en vez de al público. `Login.cshtml` es la única página que sigue sin mostrar esta navegación (no tiene sentido antes de autenticarse) — resuelto con `@if (User.Identity?.IsAuthenticated ?? false)` alrededor de los enlaces en el propio `_Layout.cshtml`, en vez de un layout distinto solo para `Login`: más simple que mantener dos layouts de Admin, y necesario de todas formas porque una sesión puede expirar mientras se navega por cualquier otra página protegida (no solo llegar directo a `Login`).

### Redirección tras iniciar sesión

`Login.cshtml.cs` (`OnGet`/`OnPostAsync`) cambia el valor por defecto de `returnUrl` de `Url.Content("~/")` a `Url.Content("~/Admin")` — hasta ahora no había ningún sitio con sentido al que llevar al administrador tras autenticarse; con la página de inicio de este incremento, sí. `LocalRedirect` sigue respetando un `returnUrl` explícito si la sesión expiró intentando acceder a una página en concreto.

### Verificación

Test de integración por entidad (`tests/BasketBaseTracker.Tests/Integration/CatalogoAdminPagesTests.cs`), reutilizando `AppHostSqlFixture` (BAS-5) para no levantar el `AppHost` una vez por test: autenticar como el administrador de *seed*, `POST` a la página `Create` con datos válidos, comprobar que aparece en `Index`, editarlo vía `Edit`, comprobar el cambio. Más un test que confirma que una petición anónima a `/Admin/Temporada` redirige a `/Admin/Login`.

**Cookies y token antifalsificación**: cada test crea su propio `HttpClient` (`AppHostSqlFixture.CreateWebHttpClient()`, un método nuevo en la fixture) en vez de reutilizar uno compartido, para no arrastrar sesión autenticada entre tests. No hace falta un `CookieContainer` manual: el `SocketsHttpHandler` por defecto de `HttpClient` ya gestiona cookies por instancia, así que basta con reutilizar el mismo `HttpClient` a lo largo de un test (login → GET `Create` → POST `Create` → GET `Edit` → POST `Edit`). El token `__RequestVerificationToken` sí hay que extraerlo a mano del HTML de cada GET (una expresión regular simple) porque viaja como campo oculto del formulario, no como cookie — usar un token de una petición anterior falla con 400 (probado).
